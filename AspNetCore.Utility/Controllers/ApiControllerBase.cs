using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore
{
    [ExceptionActionFilter]
    [ApiController]
    [ValidateModelState]
    public class ApiControllerBase : ControllerBase
    { }
}
