using SemanticGolden.Domain;

namespace SemanticGolden.Application;

/// <summary>
/// Phase 6 cross-project / cross-namespace fixture (施工包 §59).
/// Demonstrates that User.Create / User.Login can be resolved via
/// MSBuildWorkspace Compilation even when consumed from Application layer.
/// </summary>
public sealed class UserService
{
    public User CreateUser(string phoneNumber)
    {
        return User.Create(phoneNumber);
    }

    public bool Login(User user) => user.Login();
}
