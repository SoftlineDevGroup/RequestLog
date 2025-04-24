using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalMethod;

namespace sline.IntegrationCore.Server
{
  partial class ExternalMethodFunctions
  {
    [Public, Remote]
    public static sline.IntegrationCore.IExternalMethod CreateMethod()
    {
      return sline.IntegrationCore.ExternalMethods.Create();
    }
    
    [Public, Remote]
    public static IQueryable<sline.IntegrationCore.IExternalMethod> GetMethods(sline.IntegrationCore.IExternalConnection connection)
    {
      return sline.IntegrationCore.ExternalMethods.GetAll(x => Equals(x.ExternalConnection, connection));
    }
  }
}