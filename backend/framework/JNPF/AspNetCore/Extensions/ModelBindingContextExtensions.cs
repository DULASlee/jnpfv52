using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// <see cref="ModelBindingContext"/> 拓展
/// </summary>
[SuppressSniffer]
public static class ModelBindingContextExtensions
{
    /// <summary>
    /// 解析默认模型绑定
    /// </summary>
    /// <param name="bindingContext"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static async Task DefaultAsync(this ModelBindingContext bindingContext, Action<ModelBindingContext> configure = default)
    {
        // 判断模型是否已经设置
        if (bindingContext.Result.IsModelSet) return;

        // 获取绑定信息
        var bindingInfo = bindingContext.ActionContext.ActionDescriptor.Parameters.First(u => u.Name == bindingContext.OriginalModelName).BindingInfo;

        // 创建模型元数据
        var modelMetadata = bindingContext.ModelMetadata.GetMetadataForType(bindingContext.ModelType);

        // 获取模型绑定工厂对象
        var modelBinderFactory = bindingContext.HttpContext.RequestServices.GetRequiredService<IModelBinderFactory>();

        // 创建默认模型绑定器
        var modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            BindingInfo = bindingInfo,
            Metadata = modelMetadata
        });

        // 调用默认模型绑定器
        await modelBinder.BindModelAsync(bindingContext);

        // 处理回调
        configure?.Invoke(bindingContext);

        // 确保数据验证正常运行
        bindingContext.ValidationState[bindingContext.Result.Model] = new ValidationStateEntry
        {
            Metadata = bindingContext.ModelMetadata,
        };
    }
}