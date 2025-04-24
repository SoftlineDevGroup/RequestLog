using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalConnection;

namespace sline.IntegrationCore.Client
{
  partial class ExternalConnectionActions
  {
    public virtual void ShowMethods(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ExternalMethod.Remote.GetMethods(_obj).Show();
    }

    public virtual bool CanShowMethods(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && Functions.Module.CanManageSettings();
    }

    public virtual void AddMethod(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var newMethod = Functions.ExternalMethod.Remote.CreateMethod();
      newMethod.ExternalConnection = _obj;
      newMethod.ShowModal();
    }

    public virtual bool CanAddMethod(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && Functions.Module.CanManageSettings();
    }

    public virtual void SetPassword(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(Resources.SetPassword);
      var password = dialog.AddPasswordString(Resources.Password, true);
      if (dialog.Show() == DialogButtons.Ok)
        ConvertAndSetPassword(_obj.Login, password.Value);
    }
    
    public virtual void ConvertAndSetPassword(string login, string password)
    {
      if (_obj.AuthenticationType == ExternalConnection.AuthenticationType.Basic)
      {
        var authBytes = System.Text.Encoding.UTF8.GetBytes(login + ":" + password);
        string authString = System.Convert.ToBase64String(authBytes);
        Functions.ExternalConnection.SetConnectionAuth(_obj, authString);
      }
    }

    public virtual bool CanSetPassword(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.Module.CanManageSettings() && _obj.AuthenticationType == ExternalConnection.AuthenticationType.Basic;
    }

  }

}