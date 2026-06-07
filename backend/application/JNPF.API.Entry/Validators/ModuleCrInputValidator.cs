using FluentValidation;
using JNPF.Systems.Entitys.Dto.Module;

namespace JNPF.API.Entry.Validators;

public class ModuleCrInputValidator : AbstractValidator<ModuleCrInput>
{
    public ModuleCrInputValidator()
    {
        RuleFor(x => x.fullName)
            .NotEmpty().WithMessage("菜单名称不能为空")
            .MaximumLength(50).WithMessage("菜单名称最多50个字符");

        RuleFor(x => x.enCode)
            .NotEmpty().WithMessage("菜单编码不能为空")
            .MaximumLength(50).WithMessage("菜单编码最多50个字符");

        RuleFor(x => x.type)
            .NotEmpty().WithMessage("菜单类型不能为空")
            .Must(t => t == 1 || t == 2).WithMessage("菜单类型必须为1(目录)或2(页面)");

        RuleFor(x => x.category)
            .NotEmpty().WithMessage("菜单分类不能为空")
            .Must(c => c == "Web" || c == "App").WithMessage("菜单分类必须为Web或App");

        RuleFor(x => x.linkTarget)
            .Must(t => t == "_self" || t == "_blank")
            .WithMessage("链接方式必须为_self或_blank")
            .When(x => !string.IsNullOrEmpty(x.linkTarget));

        RuleFor(x => x.urlAddress)
            .NotEmpty().WithMessage("链接地址不能为空")
            .When(x => x.type == 2);

        RuleFor(x => x.description)
            .MaximumLength(200).WithMessage("描述最多200个字符");
    }
}
