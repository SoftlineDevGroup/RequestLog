using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace sline.IntegrationCore.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Асинхронник по отправке запросов в стороннюю систему
    /// </summary>
    /// <param name="Guid">Guid асинхронного обработчика</param>
    /// <param name="IsManual">Признак отправки вручную, с кнопки</param>
    /// <param name="QueryId">Если заполнен идентификатором исходящего запроса, выполняется точечная отправка</param>
    public virtual void AsyncSendRequests(sline.IntegrationCore.Server.AsyncHandlerInvokeArgs.AsyncSendRequestsInvokeArgs args)
    {
      bool isDebug = Functions.Module.GetDebugModeSetting();
      int maxCount = Functions.Module.GetIterationMaxCountSetting();
      bool hasNotSended = false;
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug($"AsyncSendRequests > Start, Guid '{args.Guid}'");
      
      var queryLog = sline.IntegrationCore.OutRequests.Null;
      long queryId = 0;
      
      #region Ручная отправка одной записи
      
      if (args.QueryId > 0)
      {
        try
        {
          queryId = args.QueryId;
          queryLog = sline.IntegrationCore.OutRequests.Get(queryId);
        }
        catch
        {
          Logger.WithLogger("IntegrationCore").Error($"AsyncSendRequests > Not found OutRequest with Id='{args.QueryId}'.");
          args.Retry = false;
        }
        
        try
        {
          if (queryLog != null)
          {
            if (args.IsManual)
              sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
            else
            {
              if (queryLog.Iteration <= maxCount)
                sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
            }
          }
          else
            args.Retry = false;
        }
        catch (Exception exc)
        {
          Logger.WithLogger("IntegrationCore").Error($"AsyncSendRequests > Critical Error: {exc.Message}, StackTrace {exc.StackTrace}");
          args.Retry = false;
        }
      }
      #endregion
      else
      {
        var outRequests = sline.IntegrationCore.OutRequests.GetAll(x => x.AsyncHandlerId == args.Guid && x.Status == sline.IntegrationCore.OutRequest.Status.InWork).ToList();
        if (outRequests != null && outRequests.Any())
        {
          foreach (var request in outRequests)
          {
            try
            {
              if (request != null)
              {
                if (Locks.GetLockInfo(request).IsLocked)
                {
                  if (isDebug)
                    Logger.WithLogger("IntegrationCore").Debug($"AsyncSendRequests > Request '{request?.Id}' is locked, skip");
                  hasNotSended = true;
                  continue;
                }
                
                if (args.IsManual)
                  sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
                else
                {
                  if (queryLog.Iteration <= maxCount)
                    sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
                }
              }
              else
                args.Retry = false;
            }
            catch(Exception exc)
            {
              Logger.WithLogger("IntegrationCore").Error($"AsyncSendRequests > Critical Error: {exc.Message}, StackTrace {exc.StackTrace}");
              args.Retry = false;
            }
          }
        }
        
        #region Альтернативный вариант отбора данных для АО - выборка из базы данных
        
        /* var arrayIds = Functions.Module.GetAsyncIds(args.Guid);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"AsyncSendRequests > RequestsId: '{arrayIds}'");
        
        if (arrayIds != null && arrayIds.Any())
        {
          foreach (var id in arrayIds)
          {
            if (!string.IsNullOrWhiteSpace(id))
            {
              try
              {
                queryId = Convert.ToInt64(id);
                queryLog = sline.IntegrationCore.OutRequests.Get(queryId);
              }
              catch
              {
                if (isDebug)
                  Logger.WithLogger("IntegrationCore").Error($"AsyncSendRequests > Not found OutRequest with Id='{id}'.");
                args.Retry = false;
              }

              try
              {
                if (queryLog != null)
                {
                  if (queryLog.AsyncHandlerId == args.Guid && queryLog.Status == sline.IntegrationCore.OutRequest.Status.InWork)
                  {
                    if (args.IsManual)
                      sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
                    else
                    {
                      if (queryLog.Iteration <= maxCount)
                        sline.IntegrationCore.PublicFunctions.Module.SendRequest(queryLog);
                    }
                  }
                }
                else
                  args.Retry = false;
              }
              catch(Exception exc)
              {
                Logger.WithLogger("IntegrationCore").Error($"AsyncSendRequests > Critical Error: {exc.Message}, StackTrace {exc.StackTrace}");
                args.Retry = false;
              }
            }
          }
        }*/
        
        #endregion
      }
      
      // если есть что-то не отправленное, повторим
      if (hasNotSended)
      {
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"AsyncSendRequests > Has not sended request, retry next time");
        
        args.Retry = true;
        args.NextRetryTime = Calendar.Now.AddMinutes(5);
      }
    }

  }
}