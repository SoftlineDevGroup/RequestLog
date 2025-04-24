using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace sline.IntegrationCore.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Удаление старых запросов из журнала интеграции
    /// </summary>
    public virtual void DeleteOldRequest()
    {
      bool isDebug = Functions.Module.GetDebugModeSetting();
      int lifeTime = Functions.Module.GetLifeTimeSetting();
      var cutoff = Calendar.Now.AddDays(-lifeTime);
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("Job > DeleteOldRequest > Start");
      
      var requests = BaseRequests.GetAll(x => x.Created <= cutoff);
      
      foreach (var request in requests.ToList())
      {
        try
        {
          if (request == null)
            continue;
          
          if (Locks.GetLockInfo(request).IsLocked)
          {
            if (isDebug)
              Logger.WithLogger("IntegrationCore").Debug($"Job > DeleteOldRequest > Request '{request?.Id}' is locked, skip");
            continue;
          }
          
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"Job > DeleteOldRequest > Delete request '{request?.Id}'");
          
          sline.IntegrationCore.BaseRequests.Delete(request);
          
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug("Job > DeleteOldRequest > Request has been deleted");
        }
        catch (Exception exc)
        {
          Logger.WithLogger("IntegrationCore").Error($"Job > DeleteOldRequest > Message: {exc.Message}, StackTrace: {exc.StackTrace}");
        }
      }
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("Job > DeleteOldRequest > End");
    }

    /// <summary>
    /// Отправка запросов из журнала интеграции, которые не были отправлены ранее.
    /// </summary>
    public virtual void SendingRequest()
    {
      bool isDebug = Functions.Module.GetDebugModeSetting();
      int iterationCount = Functions.Module.GetIterationMaxCountSetting();
      int batchCount = Functions.Module.GetAsyncBatchSetting();
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("Job > SendingRequest > Start");
      
      // собираем только активные записи
      var requests = OutRequests.GetAll(x => Equals(x.Status, sline.IntegrationCore.OutRequest.Status.Active));
      
      // только те, у которых не закончились попытки
      requests = requests.Where(x => x.Iteration <= iterationCount && x.InstantResponse != true);
      
      // первая очередь - не отправленные
      var requestsNotSended = requests.Where(x => !Equals(x.IsSend, true)).ToList();
      var requestsNotSendedCount = requestsNotSended.Count();
      var taken = 0;
      if (requestsNotSendedCount > 0)
      {
        while (taken <= requestsNotSendedCount)
        {
          var guid = Guid.NewGuid().ToString();
          var requestsTake = requestsNotSended.Skip(taken).Take(batchCount).ToList();
          foreach (var request in requestsTake)
          {
            request.AsyncHandlerId = guid;
            request.Status = sline.IntegrationCore.OutRequest.Status.InWork;
            request.Save();
          }
          
          // альтернативный вариант отбора данных для АО - выборка из базы данных
          // на случай, если предыдущая конструкция будет выполняться дольше ожидаемого
          /*var listRequestsId = string.Join(";", requestsTake.Select(x => x.Id.ToString()).ToList());
          Functions.Module.InsertAsyncIds(guid, listRequestsId);
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"Job > SendingRequest > RequestsNotSended. Guid: '{guid}', RequestsId: '{listRequestsId}'");*/
          
          var asyncRequestSending = IntegrationCore.AsyncHandlers.AsyncSendRequests.Create();
          asyncRequestSending.Guid = guid;
          asyncRequestSending.IsManual = false;
          asyncRequestSending.ExecuteAsync();
          
          taken += batchCount;
        }
      }
      
      // вторая очередь - не доставленные
      var requestsNotDelivered = requests
        .Where(x => !Equals(x.IsDelivery, true))
        .Where(x => !Equals(x.IsSend, false))
        .Where(x => !requestsNotSended.Contains(x))
        .ToList();
      
      var requestsNotDeliveredCount = requestsNotDelivered.Count();
      taken = 0;
      if (requestsNotDeliveredCount > 0)
      {
        while (taken <= requestsNotDeliveredCount)
        {
          var guid = Guid.NewGuid().ToString();
          var requestsTake = requestsNotDelivered.Skip(taken).Take(batchCount).ToList();
          foreach (var request in requestsTake)
          {
            request.AsyncHandlerId = guid;
            request.Status = sline.IntegrationCore.OutRequest.Status.InWork;
            request.Save();
          }
          
          // альтернативный вариант отбора данных для АО - выборка из базы данных
          // на случай, если предыдущая конструкция будет выполняться дольше ожидаемого
          /*var listRequestsId = string.Join(";", requestsTake.Select(x => x.Id.ToString()).ToList());
          Functions.Module.InsertAsyncIds(guid, listRequestsId);
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"Job > SendingRequest > RequestsNotDelivered. Guid: '{guid}', RequestsId: '{listRequestsId}'"); */
          
          var asyncRequestSending = IntegrationCore.AsyncHandlers.AsyncSendRequests.Create();
          asyncRequestSending.Guid = guid;
          asyncRequestSending.IsManual = false;
          asyncRequestSending.ExecuteAsync();
          
          taken += batchCount;
        }
      }
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("Job > SendingRequest > End");      
    }

  }
}