using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalConnection;

namespace sline.IntegrationCore
{
  partial class ExternalConnectionClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var needLogin = _obj.AuthenticationType == ExternalConnection.AuthenticationType.Basic;
      _obj.State.Properties.Login.IsRequired = needLogin;
      _obj.State.Properties.Password.IsRequired = needLogin;
      _obj.State.Properties.Base64.IsRequired = needLogin;
    }

  }
}