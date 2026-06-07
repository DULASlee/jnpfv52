using FluentValidation;
using JNPF.Systems.Entitys.Dto.User;

namespace JNPF.API.Entry.Validators;

public class UserCrInputValidator : AbstractValidator<UserCrInput>
{
    public UserCrInputValidator()
    {
        RuleFor(x => x.account)
            .NotEmpty().WithMessage("账号不能为空")
            .MinimumLength(3).WithMessage("账号至少3个字符")
            .MaximumLength(50).WithMessage("账号最多50个字符");

        RuleFor(x => x.realName)
            .NotEmpty().WithMessage("姓名不能为空")
            .MaximumLength(50).WithMessage("姓名最多50个字符");

        RuleFor(x => x.email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.email));

        RuleFor(x => x.mobilePhone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.mobilePhone));

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码至少6个字符")
            .MaximumLength(32).WithMessage("密码最多32个字符");

        RuleFor(x => x.organizeId)
            .NotEmpty().WithMessage("所属组织不能为空");

        RuleFor(x => x.positionId)
            .NotEmpty().WithMessage("所属岗位不能为空");
    }
}
