using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore
{
    [ExceptionActionFilter]
    [ApiController]
    [ValidateModelState]
    [ResponseApiActionFilter]
    public abstract class ApiControllerBase : ControllerBase
    {
        public virtual void AddModelError(string key, string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ModelState.AddModelError(key, errorMessage);
            }
        }

        public virtual void AddModelError(string error)
        {
            AddModelError(Guid.NewGuid().ToString(), error);
        }

        public virtual void AddModelError(List<string> errors)
        {
            foreach (var error in errors)
            {
                AddModelError(error);
            }
        }
    }
}
