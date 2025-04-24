using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.OutRequest;
using System.IO;
using System.Net;
using System.Text;

namespace sline.IntegrationCore.Client
{
  partial class OutRequestActions
  {
    public virtual void Resend(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var guid = Guid.NewGuid().ToString();
      if (_obj.Status != IntegrationCore.OutRequest.Status.Active)
        _obj.Status = IntegrationCore.OutRequest.Status.Active;
      _obj.AsyncHandlerId = guid;
      _obj.Save();
      
      var asyncRequestsSending = IntegrationCore.AsyncHandlers.AsyncSendRequests.Create();
      asyncRequestsSending.QueryId = _obj.Id;
      asyncRequestsSending.Guid = guid;
      asyncRequestsSending.IsManual = true;
      asyncRequestsSending.ExecuteAsync(sline.IntegrationCore.OutRequests.Resources.ResendInit, sline.IntegrationCore.OutRequests.Resources.ResendSending);
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanResend(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && Functions.Module.CanManageSettings();
    }

  }


}