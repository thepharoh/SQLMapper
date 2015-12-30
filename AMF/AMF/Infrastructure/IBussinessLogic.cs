using System;
using System.Web.Mvc;

namespace AMF.Infrastructure
{
    public interface IBussinessLogic
    {
        bool Validate(ModelStateDictionary ModelState);
    }
}