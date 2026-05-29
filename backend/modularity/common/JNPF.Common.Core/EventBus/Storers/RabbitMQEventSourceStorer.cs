using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.EventBus;
using JNPF.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace JNPF.EventHandler;

/// <summary>
/// RabbitMQ 事件存储器.
/// </summary>
public sealed class RabbitMQEventSourceStorer : IEventSourceStorer, IDisposable
{
    /// <summary>
    /// 内存通道事件源存储器.
    /// </summary>
    private readonly Channel<IEventSource> _channel;

    /// <summary>
    /// 通道对象.
    /// </summary>
    private readonly IModel _model;

    /// <summary>
    /// 连接对象.
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    /// 路由键.
    /// </summary>
    private readonly string _routeKey;

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="factory">连接工厂.</param>
    /// <param name="routeKey">路由键.</param>
    /// <param name="capacity">存储器最多能够处理多少消息，超过该容量进入等待写入.</param>
    public RabbitMQEventSourceStorer(ConnectionFactory factory, string routeKey, int capacity)
    {
        // 配置通道，设置超出默认容量后进入等待
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        // 创建有限容量通道
        _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);

        // 创建连接
        _connection = factory.CreateConnection();

        _routeKey = routeKey;

        // 创建通道
        _model = _connection.CreateModel();

        /*
         * 声明路由队列
         * 队列可以重复声明，但是声明所使用的参数必须一致，否则会抛出异常
         * queue：队列名称
         * durable：持久化
         * exclusive：排他队列，如果一个队列被声明为排他队列，该队列仅对首次声明它的连接可见，
         * 并在连接断开时自动删除。这里需要注意三点：其一，排他队列是基于连接可见的，同一连接的不同信道是可
         * 以同时访问同一个连接创建的排他队列的。其二，“首次”，如果一个连接已经声明了一个排他队列，其他连
         * 接是不允许建立同名的排他队列的，这个与普通队列不同。其三，即使该队列是持久化的，一旦连接关闭或者
         * 客户端退出，该排他队列都会被自动删除的。这种队列适用于只限于一个客户端发送读取消息的应用场景。
         * autoDelete：自动删除
         * arguments：参数
         */
        _model.QueueDeclare(queue: routeKey, durable: false, exclusive: false, autoDelete: false, arguments: null);

        // 将MaxKey 交换机绑定到 路由中
        // _model.QueueBind(queue: routeKey, exchange: "MXK_IDENTITY_MAIN_TOPIC", routingKey: "#");

        // 字节限制，一次接收的消息数，全局/
        // 据说prefetchSize 和global这两项，rabbitmq没有实现，暂且不研究
        _model.BasicQos(prefetchSize: 0, prefetchCount: 1, false);

        // 创建消息订阅者
        var consumer = new EventingBasicConsumer(_model);

        // 订阅消息并写入内存 Channel
        consumer.Received += (ch, ea) =>
        {
            // 读取原始消息
            var stringEventSource = Encoding.UTF8.GetString(ea.Body.ToArray());

            // 转换为 IEventSource，这里可以选择自己喜欢的序列化工具，如果自定义了 EventSource，注意属性是可读可写
            var eventSource = JsonSerializer.Deserialize<ChannelEventSource>(stringEventSource);

            // 判断到是单点登录服务端信息
            if (eventSource.EventId.IsNullOrEmpty()) eventSource = new ChannelEventSource("User:Maxkey_Identity", stringEventSource);

            Log.Information($"- 接收到消息：{eventSource.ToJsonString()}");

            // 写入内存管道存储器
            _channel.Writer.TryWrite(eventSource);

            // 确认该消息已被消费
            _model.BasicAck(ea.DeliveryTag, false);
        };

        // 启动消费者 设置为手动应答消息
        _model.BasicConsume(queue: routeKey, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 将事件源写入存储器.
    /// </summary>
    /// <param name="eventSource">事件源对象.</param>
    /// <param name="cancellationToken">取消任务 Token.</param>
    /// <returns><see cref="ValueTask"/></returns>
    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        // 空检查
        if (eventSource == default)
        {
            throw new ArgumentNullException(nameof(eventSource));
        }

        // 这里判断是否是 ChannelEventSource 或者 自定义的 EventSource
        if (eventSource is ChannelEventSource source)
        {
            // 序列化，这里可以选择自己喜欢的序列化工具
            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(source));

            // 发布
            _model.BasicPublish(string.Empty, _routeKey, basicProperties: null, body: data);
        }
        else
        {
            // 这里处理动态订阅问题
            await _channel.Writer.WriteAsync(eventSource, cancellationToken);
        }
    }

    /// <summary>
    /// 从存储器中读取一条事件源.
    /// </summary>
    /// <param name="cancellationToken">取消任务 Token.</param>
    /// <returns>事件源对象.</returns>
    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
        // 读取一条事件源
        var eventSource = await _channel.Reader.ReadAsync(cancellationToken);
        return eventSource;
    }

    /// <summary>
    /// 释放非托管资源.
    /// </summary>
    public void Dispose()
    {
        _model.Dispose();
        _connection.Dispose();
    }
}