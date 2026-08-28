// R1 Attack Fixture — 专用于证明原始 R1 方案的潜在错误不会重现。
// 本文件不属于 JNPF 生产代码，仅被 JnpfAnalyzer 作为被测输入。
// 覆盖：Attack A(Overload) / B(同名不同类) / C(Extension) / D(Interface) / E(Reverse Edge) / F(Unresolved)
//       以及 R1-06 Generic / R1-08 Async / R1-10 Virtual / R1-11 Cross-Class / R1-13 Line

namespace R1Fixtures;

// ── Attack A / R1-C：同名 overload 不得因方法名合并 ──────────────────
public class OverloadFoo
{
    public string Save(int id) => "int:" + id;
    public string Save(string id) => "str:" + id;
    public string Save(Guid id) => "guid:" + id;

    // 调用三个重载，各自应产生指向不同 target_method_id 的边
    public void Caller()
    {
        Save(1);
        Save("x");
        Save(Guid.Empty);
    }
}

// ── Attack B：同名不同类不得混淆 ────────────────────────────────────
public class Alpha { public void Save() { } }
public class Beta { public void Save() { } }
public class SameNameCrossClass
{
    public void Caller()
    {
        new Alpha().Save();
        new Beta().Save();
    }
}

// ── Attack C / R1-D：扩展方法必须解析到实际定义 Symbol ──────────────
public static class QueryExtensions
{
    public static List<T> ToListAsync<T>(this IEnumerable<T> src) => src.ToList();
    public static Task<List<T>> ToListAsyncAsync<T>(this IEnumerable<T> src) => Task.FromResult(src.ToList());
}
public class ExtensionCaller
{
    public List<int> LinqStyle()
    {
        var query = new[] { 1, 2, 3 }.Where(x => x > 1);   // LINQ 扩展
        return query.ToListAsync();                          // 自定义扩展
    }
}

// ── Attack D / R1-E：接口调用不得声称唯一实现 ───────────────────────
public interface IFileManager { void DeleteFile(string path); }
public class LocalFileManager : IFileManager { public void DeleteFile(string path) { } }
public class CloudFileManager : IFileManager { public void DeleteFile(string path) { } }
public class InterfaceConsumer
{
    private readonly IFileManager _fm;
    public InterfaceConsumer(IFileManager fm) => _fm = fm;
    public void Remove(string p) => _fm.DeleteFile(p);   // 目标须=IFileManager.DeleteFile，status=Partial
}

// ── Attack E / R1-B：反向边多调用者不得丢失/错归属 ──────────────────
public class SharedTarget { public void BeCalled() { } }
public class CallerOne
{
    public void Go(SharedTarget t) { t.BeCalled(); }
}
public class CallerTwo
{
    public void Go(SharedTarget t) { t.BeCalled(); }
}

// ── Attack F：无法静态确定时不得猜，标 Partial/Unresolved ───────────
public class UnresolvedCaller
{
    // dynamic：静态无法解析目标 Symbol
    public dynamic Dyn(dynamic obj) => obj.MagicCall(123);
    // 完全未知的标识符（编译错误场景本 fixture 不放，避免污染；用委托模拟）
    public void DelegateInvoke(Func<int, int> f) { f(1); }   // 委托调用 → Delegate/Partial
}

// ── R1-06：泛型方法，构造版本须映射回定义 ID ────────────────────────
public class GenericHolder
{
    public T Echo<T>(T value) => value;
    public void Caller()
    {
        Echo(42);
        Echo("text");
        Echo(Guid.NewGuid());
    }
}

// ── R1-08：async/await 链 ───────────────────────────────────────────
public class AsyncCaller
{
    public async Task<string> LoadAsync()
    {
        await Task.Delay(1);
        return await InnerAsync();
    }
    private async Task<string> InnerAsync()
    {
        await Task.Yield();
        return "done";
    }
}

// ── R1-10：虚方法调用（不违反 override 解析）────────────────────────
public class BaseWorker { public virtual void Work() { } }
public class DerivedWorker : BaseWorker { public override void Work() { } }
public class VirtualCaller
{
    public void Drive(BaseWorker w) { w.Work(); }   // Virtual/Partial（静态不知实际运行时类型）
}

// ── R1-11：跨类调用链（模拟 Ground Truth 结构：Service→Manager→IO）──
public class IoHelper { public void Write(string s) { } }
public class DataManager
{
    private readonly IoHelper _io = new();
    public void Persist(string key) { _io.Write(key); }
}
public class LeafService
{
    private readonly DataManager _dm = new();
    public void Handle(string k) { _dm.Persist(k); }   // 跨类：LeafService.Handle → DataManager.Persist
}

// ── R1-12：calledBy 同时含正向跨层，验证正反边对称 ──────────────────
public class RootEntry
{
    public void Start() { new LeafService().Handle("a"); }
}
