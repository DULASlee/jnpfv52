using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
var malicious = new JObject();
malicious[$type] = \ System.Diagnostics.Process, System\";
malicious[\"FileName\"] = \"cmd.exe\";
try {
 var result = malicious.ToObject<System.Collections.Generic.Dictionary<string, string>>();
 Console.WriteLine(\"JToken.ToObject result: \ + result.Count + \ keys\);
    foreach (var kv in result) Console.WriteLine(\ \ + kv.Key + \ = \ + kv.Value);
    Console.WriteLine(\SAFE: $type treated as string value, not type resolution\);
} catch (Exception ex) {
    Console.WriteLine(\EXCEPTION: \ + ex.Message);
}
