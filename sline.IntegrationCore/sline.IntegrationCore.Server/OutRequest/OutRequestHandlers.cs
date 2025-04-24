using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using sline.IntegrationCore.OutRequest;

namespace sline.IntegrationCore
{
  partial class OutRequestServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      if (!isUpdateAction)
        return;
      
      var properties = _obj.State.Properties.Where(p =>
                                                   p == _obj.State.Properties.DeliveryDatetime ||
                                                   p == _obj.State.Properties.SendDatetime ||
                                                   p == _obj.State.Properties.Iteration ||
                                                   p == _obj.State.Properties.StatusCode);
      
      var propertiesType = OutRequests.Info.Properties.GetType();
      var objType = _obj.GetType();
      
      foreach (var property in properties)
      {
        var isChanged = (property as IPropertyState).IsChanged;
        if (isChanged)
        {
          var propertyName = (property as PropertyStateBase).PropertyName;
          var propertyInfo = propertiesType.GetProperty(propertyName).GetValue(OutRequests.Info.Properties);
          var name = propertyInfo.GetType().GetProperty("LocalizedName").GetValue(propertyInfo);          
          var newValue = objType.GetProperty(propertyName).GetValue(_obj);
          var oldValue = property.GetType().GetProperty("OriginalValue").GetValue(property);
          
          if (newValue == oldValue ||
              newValue != null && oldValue != null && Equals(newValue.ToString(), oldValue.ToString()) ||
              newValue != null && oldValue == null && string.IsNullOrEmpty(newValue.ToString()) ||
              oldValue != null && newValue == null && string.IsNullOrEmpty(oldValue.ToString()))
            continue;
          
          var comment = string.Format("{0}: '{1}' => '{2}'", name, newValue, oldValue);
          e.Write(e.Operation, null, comment);
        }
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.IsDelivery = false;
      _obj.IsSend = false;
      _obj.Iteration = 0;
      _obj.InstantResponse = false;
    }
  }

}