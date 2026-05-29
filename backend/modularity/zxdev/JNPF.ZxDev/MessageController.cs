using JNPF.Common.Core.Manager;
using JNPF.Common.CodeGen.DataParsing;
using JNPF.Common.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;


using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using JNPF.ZxDev.Entitys;
using JNPF.Common.Helper;
using System.Data;
using JNPF.Common.Configuration;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.Text;
using JNPF.ZxDev.Entitys.Dto.Config;




namespace JNPF.ZxDev
{
    [ApiDescriptionSettings("ZxDev", Tag = "Config", Name = "Config", Order = 200)]
    [Route("api/ZxDev/[controller]")]
    public class MessageController : IDynamicApiController, ITransient
    {

    }
}
