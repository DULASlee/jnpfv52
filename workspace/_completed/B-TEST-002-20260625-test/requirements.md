# 需求

为订单模块新增 Email 字段：
1. OrderEntity 增加 Email 属性（string, 必填, 最大长度200）
2. OrderDto 增加 Email 属性
3. OrderService.CreateOrder 增加 Email 格式校验（正则）
4. 更新相关单元测试

预计修改文件：4个
涉及：Entity + DTO + Service
