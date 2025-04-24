using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.BaseRequest;

namespace sline.IntegrationCore.Client
{
  partial class BaseRequestActions
  {
    public virtual void OpenEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var entity = Functions.BaseRequest.Remote.SelectEntityForOpen(_obj);
      if (entity != null)
        entity.ShowModal();
      else
        e.AddError(sline.IntegrationCore.BaseRequests.Resources.ErrorCantOpenEntity);
    }

    public virtual bool CanOpenEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !string.IsNullOrWhiteSpace(_obj.EntityType) && _obj.EntityId != null;
    }

    public virtual void ShowLargeBody(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var largeBody = _obj.LargeBody;
      var body = largeBody.Body;
      
      var dialog = Dialogs.CreateInputDialog(sline.IntegrationCore.BaseRequests.Resources.RequestBodyName);
      dialog.AddString(sline.IntegrationCore.BaseRequests.Resources.BodyName, false, body);
      dialog.Width = 300;
      dialog.Height = 500;
      dialog.Buttons.AddOk();
      dialog.Show();      
    }

    public virtual bool CanShowLargeBody(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.LargeBody != null;
    }

  }

}