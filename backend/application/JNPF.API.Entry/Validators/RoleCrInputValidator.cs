using FluentValidation;
using JNPF.Systems.Entitys.Dto.Role;

namespace JNPF.API.Entry.Validators;

public class RoleCrInputValidator : AbstractValidator<RoleCrInput>
{
    public RoleCrInputValidator()
    {
        RuleFor(x => x.fullName)
            .NotEmpty().WithMessage("角色名称不能为空")
            .MaximumLength(50).WithMessage("角色名称最多50个字符");

        RuleFor(x => x.enCode)
            .NotEmpty().WithMessage("角色编码不能为空")
            .MaximumLength(50).WithMessage("角色编码最多50个字符")
            .Matches(@"^[a-zA-Z_][a-zA-Z0-9_]*$").WithMessage("角色编码只能包含字母、数字和下划线，且以字母或下划线开头");

        RuleFor(x => x.type)
            .NotEmpty().WithMessage("角色类型不能为空");

        RuleFor(x => x.description)
            .MaximumLength(200).WithMessage("描述最多200个字符");
    }
}
