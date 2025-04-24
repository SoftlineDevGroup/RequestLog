using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalConnection;

namespace sline.IntegrationCore.Shared
{
  partial class ExternalConnectionFunctions
  {
    public void SetConnectionAuth(string auth)
    {
      //_obj.Save();
      _obj.Base64 = auth;
      _obj.Save();
    }
  }
}