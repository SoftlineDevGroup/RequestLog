using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.BaseRequest;

namespace sline.IntegrationCore.Server
{
  partial class BaseRequestFunctions
  {
    [Remote]
    public Sungero.Domain.Shared.IEntity SelectEntityForOpen()
    {
      try
      {
        using (var session = new Sungero.Domain.Session())
        {
          var innerSession = (Sungero.Domain.ISession)session.GetType()
            .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
          
          var guid = Guid.Parse(_obj.EntityType);
          System.Type type = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(guid);

          return (Sungero.Domain.Shared.IEntity)innerSession.Get(type, _obj.EntityId.Value);
        }
      }
      catch (Exception exc)
      {
        Logger.WithLogger("IntegrationCore").Error($"OpenEntity > EntityType: '{_obj.EntityType}', EntityId: '{_obj.EntityId}', Message: {exc.Message}, StackTrace: {exc.StackTrace}");
      }
      
      return null;
    }
  }
}