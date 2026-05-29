using JavaScriptEngineSwitcher.V8;

namespace JNPF.Extras.Thirdparty.JSEngine;

/// <summary>
/// js处理引擎.
/// </summary>
public static class JsEngineUtil
{
    private static V8JsEngine engine = new V8JsEngine();

    /// <summary>
    /// 执行Js(返回结果请用result).
    /// 如：var result = function(a,b){}.
    /// </summary>
    /// <param name="jsContent">js内容.</param>
    /// <param name="args">参数.</param>
    /// <returns></returns>
    public static object CallFunction(string jsContent, params object[] args)
    {
        try
        {
            engine.Execute(jsContent);
            return engine.CallFunction("result", args);
        }
        catch (Exception e)
        {
            throw new Exception("不支持的JS数据");
        }
    }

    /// <summary>
    /// 执行聚合函数Js(返回结果请用result).
    /// 如：var result = function(a,b){}.
    /// </summary>
    /// <param name="jsContent">js内容.</param>
    /// <param name="args">参数.</param>
    /// <returns></returns>
    public static object AggreFunction(string jsContent, params object[] args)
    {
        try
        {
            var aggreFunc = "function getNum(val) {\n" +
                    "  return isNaN(val) ? 0 : Number(val)\n" +
                    "};\n" +
                    "// 求和\n" +
                    "function SUM() {\n" +
                    "  var value = 0\n" +
                    "  for (var i = 0; i < arguments.length; i++) {\n" +
                    "    value += getNum(arguments[i])\n" +
                    "  }\n" +
                    "  return value\n" +
                    "};\n" +
                    "// 求差\n" +
                    "function SUBTRACT(num1, num2) {\n" +
                    "  return getNum(num1) - getNum(num2)\n" +
                    "};\n" +
                    "// 相乘\n" +
                    "function PRODUCT() {\n" +
                    "  var value = 1\n" +
                    "  for (var i = 0; i < arguments.length; i++) {\n" +
                    "    value = value * getNum(arguments[i])\n" +
                    "  }\n" +
                    "  return value\n" +
                    "};\n" +
                    "// 相除\n" +
                    "function DIVIDE(num1, num2) {\n" +
                    "  return getNum(num1) / (getNum(num2) === 0 ? 1 : getNum(num2))\n" +
                    "};\n" +
                    "// 获取参数的数量\n" +
                    "function COUNT() {\n" +
                    "  return arguments.length\n" +
                    "};\n";
            aggreFunc = aggreFunc + "var result =function(){ return " + jsContent + "}";

            engine.Execute(aggreFunc);
            return engine.CallFunction("result", args);
        }
        catch (Exception e)
        {
            return "";
        }
    }
}