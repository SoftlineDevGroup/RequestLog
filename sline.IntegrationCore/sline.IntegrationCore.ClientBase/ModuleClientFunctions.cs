using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace sline.IntegrationCore.Client
{
  public class ModuleFunctions
  {
    [Public]
    public virtual bool CanManageSettings()
    {
      var adminRole = Roles.Administrators;
      var integrationRole = Functions.Module.Remote.GetIntegrationRole();
      var currentUser = Users.Current;
      
      return currentUser.IncludedIn(adminRole) || (integrationRole != null && currentUser.IncludedIn(integrationRole));
    }

    /// <summary>
    /// Установка настроек модуля
    /// </summary>
    public virtual void ShowIntegrationSettings()
    {
      if (!CanManageSettings())
      {
        Dialogs.NotifyMessage(sline.IntegrationCore.Resources.Setting_NoRights);
        return;
      }
      
      int iteration = Functions.Module.Remote.GetIterationMaxCountSetting();
      int batch = Functions.Module.Remote.GetAsyncBatchSetting();
      bool debugMode = Functions.Module.Remote.GetDebugModeSetting();
      bool logIncomingRequests = Functions.Module.Remote.GetIncomingRequestSetting();
      int lifeTime = Functions.Module.Remote.GetLifeTimeSetting();
      
      var dialog = Dialogs.CreateInputDialog(sline.IntegrationCore.Resources.SettingDialogName);
      
      var hyperlinkCommons = dialog.AddHyperlink(sline.IntegrationCore.Resources.GroupGeneral);
      // режим отладки
      var debugControl = dialog.AddSelect(sline.IntegrationCore.Resources.SettingDialogDebug, true,
                                          debugMode ? sline.IntegrationCore.Resources.Dialog_Yes : sline.IntegrationCore.Resources.Dialog_No)
        .From(sline.IntegrationCore.Resources.Dialog_No, sline.IntegrationCore.Resources.Dialog_Yes);
      var lifeTimeControl = dialog.AddInteger(sline.IntegrationCore.Resources.SettingDialogLifeTime, true, lifeTime);
      
      // входящие запросы
      var hyperlinkIncoming = dialog.AddHyperlink(sline.IntegrationCore.Resources.IncomingGroup);
      var incomingRequestControl = dialog.AddSelect(sline.IntegrationCore.Resources.SettingDialogIncRequests, true,
                                                    logIncomingRequests ? sline.IntegrationCore.Resources.Dialog_Yes : sline.IntegrationCore.Resources.Dialog_No)
        .From(sline.IntegrationCore.Resources.Dialog_No, sline.IntegrationCore.Resources.Dialog_Yes);      
      
      // исходящие запросы
      var hyperlinkOutgoing = dialog.AddHyperlink(sline.IntegrationCore.Resources.OutgoingGroup);
      var iterationControl = dialog.AddInteger(sline.IntegrationCore.Resources.SettingDialogInterationName, true, iteration);
      var batchControl = dialog.AddInteger(sline.IntegrationCore.Resources.SettingDialogBatchName, true, batch);
      
      var updateButton = dialog.Buttons.AddCustom(sline.IntegrationCore.Resources.SettingDialogButtonUpdate);
      dialog.Buttons.AddCancel();
      
      if (dialog.Show() == updateButton)
      {
        if (iterationControl.Value > 0 && iterationControl.Value != iteration)
          Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.IterationMaxCountParamName, iterationControl.Value.ToString());
        
        if (batchControl.Value >= 10 && batchControl.Value != batch)
          Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.AsyncBatchParamName, batchControl.Value.ToString());
        
        if (lifeTimeControl.Value > 0 && lifeTimeControl.Value != lifeTime)
          Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.LifeTimeParamName, lifeTimeControl.Value.ToString());

        var controlValue = debugControl.Value == sline.IntegrationCore.Resources.Dialog_Yes ? true : false;
        if (controlValue != debugMode)
          Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.DebugModeParamName, controlValue.ToString());
        
        controlValue = incomingRequestControl.Value == sline.IntegrationCore.Resources.Dialog_Yes ? true : false;
        if (controlValue != logIncomingRequests)
          Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.IncomingRequestParamName, controlValue.ToString());        
      }      
      
    }

  }
}