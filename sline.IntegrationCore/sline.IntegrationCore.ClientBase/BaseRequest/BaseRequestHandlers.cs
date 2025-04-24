using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.BaseRequest;

namespace sline.IntegrationCore
{
  partial class BaseRequestClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.LargeBody.IsVisible = _obj.LargeBody != null;
    }

  }
}