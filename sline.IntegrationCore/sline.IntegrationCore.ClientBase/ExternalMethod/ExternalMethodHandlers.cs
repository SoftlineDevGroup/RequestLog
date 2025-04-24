using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalMethod;

namespace sline.IntegrationCore
{
  partial class ExternalMethodClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (_obj.ExternalConnection != null && !string.IsNullOrWhiteSpace(_obj.ExternalConnection.Url))
        e.AddInformation(sline.IntegrationCore.ExternalMethods.Resources.SummaryURLFormat(_obj.ExternalConnection.Url, _obj.Url));
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.ExternalConnection != null && !string.IsNullOrWhiteSpace(_obj.ExternalConnection.Url))
        e.AddInformation(sline.IntegrationCore.ExternalMethods.Resources.SummaryURLFormat(_obj.ExternalConnection.Url, _obj.Url));
    }

  }
}