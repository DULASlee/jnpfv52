using FluentValidation;
using JNPF.OAuth.Dto;

namespace JNPF.API.Entry.Validators;

public class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(x => x.account)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(50).WithMessage("用户名最多50个字符");

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("密码不能为空")
            .When(x => !x.isSocialsLoginCallBack);
    }
}
