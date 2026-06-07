using FluentValidation;
using JNPF.WorkFlow.Entitys.Dto.FlowForm;

namespace JNPF.API.Entry.Validators;

public class FlowFormInputValidator : AbstractValidator<FlowFormInput>
{
    public FlowFormInputValidator()
    {
        RuleFor(x => x.fullName)
            .NotEmpty().WithMessage("流程名称不能为空")
            .MaximumLength(100).WithMessage("流程名称最多100个字符");

        RuleFor(x => x.enCode)
            .NotEmpty().WithMessage("流程编码不能为空")
            .MaximumLength(100).WithMessage("流程编码最多100个字符");

        RuleFor(x => x.formType)
            .NotEmpty().WithMessage("表单类型不能为空");

        RuleFor(x => x.category)
            .NotEmpty().WithMessage("流程分类不能为空");

        RuleFor(x => x.description)
            .MaximumLength(500).WithMessage("说明最多500个字符");

        RuleFor(x => x.flowTemplateJson)
            .NotEmpty().WithMessage("流程设计JSON不能为空")
            .When(x => x.type == 0);
    }
}
