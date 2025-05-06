using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Sungero.Commons;
using Sungero.Parties;
using Sungero.Company;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace sline.IntegrationCore.Server
{
  public class ModuleFunctions
  {
    
    #region Исходящие запросы
    
    /// <summary>
    /// Запуск вызова в стороннюю систему
    /// </summary>
    [Public]
    public virtual sline.IntegrationCore.IOutRequest SendRequest(sline.IntegrationCore.IOutRequest outRequest)
    {
      bool isDebug = GetDebugModeSetting();
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug($"SendRequest > Start, OutRequest: '{outRequest?.Id}'");
      
      string result = string.Empty;
      string requestUrl = string.Empty;
      
      if (outRequest.SendTo != null)
      {
        var connection = outRequest.SendTo;
        requestUrl = connection.Url;
      }
      else
      {
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Error($"SendRequest > {sline.IntegrationCore.Resources.ExternalSystemNotSpecified}");
        
        throw new NullReferenceException(sline.IntegrationCore.Resources.ExternalSystemNotSpecified);
      }
      
      if (outRequest.Method != null)
        requestUrl += $"/{outRequest.Method.Url}";
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Error($"SendRequest > RequestUrl: {requestUrl}");
      
      try
      {
        HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
        
        foreach (var header in outRequest.Headers)
          webRequest.Headers.Set(header.Key, header.Value);
        
        webRequest.Method = outRequest.MethodType;
        
        try
        {
          if (outRequest.MethodType == sline.IntegrationCore.ExternalMethod.MethodType.POST.ToString())
          {
            using (var requestStream = webRequest.GetRequestStream())
            {
              using (var writer = new StreamWriter(requestStream))
              {
                writer.Write(outRequest.Body);
                SetRequestSended(outRequest);
                
                if (isDebug)
                  Logger.WithLogger("IntegrationCore").Debug($"SendRequest > Request sended");
              }
            }
          }
          
          try
          {
            using (var response = (HttpWebResponse)webRequest.GetResponse())
            {
              outRequest.StatusCode = response.StatusCode.ToString();
              using (var responseStream = response.GetResponseStream())
              {
                using (var reader = new StreamReader(responseStream))
                {
                  result = reader.ReadToEnd();
                  if (GetSuccessStatus(response.StatusCode))
                  {
                    SetRequestDelivered(outRequest, result);
                    
                    if (isDebug)
                      Logger.WithLogger("IntegrationCore").Debug($"SendRequest > Request delivered");
                  }
                  else
                    outRequest.Answer = result;
                }
              }
            }
          }
          catch (WebException exc)
          {
            var webResponse = (HttpWebResponse)exc.Response;
            if (webResponse != null)
            {
              outRequest.StatusCode = webResponse.StatusCode.ToString();
              using (var responseStream = webResponse.GetResponseStream())
              {
                using (var reader = new StreamReader(responseStream))
                {
                  string response = reader.ReadToEnd();
                  result = sline.IntegrationCore.Resources.OutRequest_WebExceptionFormat(response);
                  outRequest.Answer = result;
                  
                  if (isDebug)
                    Logger.WithLogger("IntegrationCore").Error($"SendRequest > WebException > StatusCode: '{webResponse.StatusCode.ToString()}', server answer: '{result}'");
                }
              }
            }
            else
            {
              result = sline.IntegrationCore.Resources.OutRequest_WebExceptionNoResponseFormat(exc.Message);
              outRequest.StatusCode = string.Empty;
              outRequest.Answer = result;
              
              if (isDebug)
                Logger.WithLogger("IntegrationCore").Error($"SendRequest > WebException > Server answer: '{exc.Message}'");
            }
          }
        }
        catch (Exception exc)
        {
          result = sline.IntegrationCore.Resources.OutRequest_OtherExceptionFormat(exc.Message);
          outRequest.StatusCode = string.Empty;
          outRequest.Answer = result;
          
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Error($"SendRequest > Exception > Other exception. Message: '{exc.Message}', StackTrace: {exc.StackTrace}");
        }
        
        if (outRequest.State.IsChanged)
          outRequest.Save();
      }
      catch (Exception exc)
      {
        Logger.WithLogger("IntegrationCore").Error($"SendRequest > Message: {exc.Message}");
      }
      
      return outRequest;
    }
    
    #region Вспомогательные методы для исходящих
    
    /// <summary>
    /// Разобрать статус ответа от сервера.
    /// </summary>
    /// <param name="statusCode">Статус ответа</param>
    /// <returns>True - если ответ успешный</returns>
    public virtual bool GetSuccessStatus(System.Net.HttpStatusCode statusCode)
    {
      return statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Accepted;
    }
    /// <summary>
    ///  Установить признак - Отправлен.
    /// </summary>
    /// <param name="outRequest"></param>
    public virtual void SetRequestSended(IOutRequest outRequest)
    {
      outRequest.IsSend = true;
      outRequest.Iteration++;
      outRequest.SendDatetime = Calendar.Now;
    }
    /// <summary>
    ///  Установить признак - Доставлен, и записать ответ сервера.
    /// </summary>
    /// <param name="outRequest"></param>
    /// <param name="answer"></param>
    public virtual void SetRequestDelivered(IOutRequest outRequest, string answer)
    {
      outRequest.Answer = answer;
      outRequest.IsDelivery = true;
      outRequest.Status = sline.IntegrationCore.OutRequest.Status.Completed;
      outRequest.DeliveryDatetime = Calendar.Now;
    }
    
    #endregion
    
    /// <summary>
    /// Создать запись журнала исходящих запросов, чтобы использовать его Id. Запись создается закрытой.
    /// </summary>
    /// <param name="data">Краткая структура с данными</param>
    /// <returns>Id</returns>
    [Public]
    public virtual long CreateOutRequest(Structures.Module.IShortData shortData)
    {
      // найдем незавершенные записи-дубликаты и закроем их
      var outRequests = OutRequests.GetAll().Where(x => Equals(x.EntityId, shortData.EntityId)
                                                   && Equals(x.EntityType, shortData.EntityType)
                                                   && !Equals(x.Status, sline.IntegrationCore.OutRequest.Status.Completed));
      foreach (var ent in outRequests)
      {
        ent.Status = sline.IntegrationCore.OutRequest.Status.Closed;
        ent.Save();
      }
      
      // создадим новую запись
      var outRequest = sline.IntegrationCore.OutRequests.Create();
      outRequest.Status = sline.IntegrationCore.OutRequest.Status.Closed;
      outRequest.EntityId = shortData.EntityId;
      outRequest.Name = shortData.Name;
      outRequest.EntityType = shortData.EntityType;
      outRequest.Save();
      
      return outRequest.Id;
    }
    
    /// <summary>
    /// Проверить, есть ли запись в работе
    /// </summary>
    /// <param name="shortData">Краткая структура с данными</param>
    /// <returns>true - если в записи указан GUID асинхронного обработчика, false - если запись не взята в работу</returns>
    [Public]
    public virtual bool GetInProcessOutRequest(Structures.Module.IShortData shortData)
    {
      var outRequests = OutRequests.GetAll()
        .Where(x => Equals(x.EntityId, shortData.EntityId)
               && Equals(x.EntityType, shortData.EntityType)
               && !Equals(x.Status, sline.IntegrationCore.OutRequest.Status.Completed)
               && !Equals(x.Status, sline.IntegrationCore.OutRequest.Status.Closed));
      return outRequests.Any(x => x.AsyncHandlerId != null || x.AsyncHandlerId != string.Empty || x.AsyncHandlerId != "");
    }
    
    /// <summary>
    /// Получить или создать запись журнала исходящих запросов. При передаче Id, будет обновлена существующая запись. Иначе - создана новая.
    /// </summary>
    /// <param name="data">Структура с данными</param>
    [Public]
    public virtual sline.IntegrationCore.IOutRequest GetOrCreateOutRequest(Structures.Module.IData data)
    {
      bool isDebug = GetDebugModeSetting();
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("GetOrCreateOutRequest > Start");
      
      var outRequest = sline.IntegrationCore.OutRequests.Null;
      try
      {
        if (data.Id == null || data.Id == 0)
        {
          outRequest = sline.IntegrationCore.OutRequests.Create();
          
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"GetOrCreateOutRequest > Create new request: '{outRequest?.Id}'");
        }
        else
        {
          outRequest = sline.IntegrationCore.OutRequests.Get(data.Id.Value);
          outRequest.Status = sline.IntegrationCore.OutRequest.Status.Active;
          
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"GetOrCreateOutRequest > Find existing request: '{outRequest?.Id}'");
        }
        
        if (!string.IsNullOrEmpty(data.Name))
          outRequest.Name = data.Name;
        
        SetSystemAndMethod(outRequest, data);
        
        if (data.Headers.Any())
        {
          foreach (var header in data.Headers)
          {
            var headerRow = outRequest.Headers.AddNew();
            headerRow.Key = header.Key;
            headerRow.Value = header.Value;
          }
        }
        
        outRequest.InstantResponse = data.InstantResponse;
        outRequest.IsSend = false;
        outRequest.IsDelivery = false;
        outRequest.Save();
        
        if (outRequest.InstantResponse == true)
        {
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug("GetOrCreateOutRequest > AsyncSendQuery start immediately.");
          
          outRequest = sline.IntegrationCore.PublicFunctions.Module.SendRequest(outRequest);
        }
      }
      catch(Exception exc)
      {
        Logger.WithLogger("IntegrationCore").Error($"GetOrCreateOutRequest > Error. Message: {exc.Message}, StackTrace: {exc.StackTrace}");
      }
      
      if (isDebug)
        Logger.WithLogger("IntegrationCore").Debug("GetOrCreateOutRequest > End");
      
      return outRequest;
    }
    
    /// <summary>
    /// Установить подключение и метод
    /// </summary>
    public virtual void SetSystemAndMethod(sline.IntegrationCore.IOutRequest outRequest, Structures.Module.IData data)
    {
      outRequest.SendTo = sline.IntegrationCore.ExternalConnections.GetAll(x => Equals(x.Code, data.SystemCode)).FirstOrDefault();
      outRequest.Method = sline.IntegrationCore.ExternalMethods.GetAll(x => Equals(x.Code, data.MethodCode)).FirstOrDefault();
    }
    /// <summary>
    /// Интеграционный метод обновления существующей записи исходящих запросов.
    /// </summary>
    /// <param name="Id">Id записи журнала исходящих запросов</param>
    /// <param name="SystemName">Имя (идентификатор) сторонней системы</param>
    /// <param name="Status">Статус запроса</param>
    /// <param name="Answer">Ответ сторонней системы</param>
    /// <param name="DateTime">Дата и время ответа</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult UpdateOutRequest(Structures.Module.IAnswersFromOtherSystems answer)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(answer);
      var now = Calendar.Now;
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("UpdateOutRequest > Start");
        Logger.WithLogger("IntegrationCore").Debug($"UpdateOutRequest > Request: '{incomingString}'");
      }
      
      var syncResult = CreateResult(answer.Id);
      var outRequest = sline.IntegrationCore.OutRequests.Null;
      
      try
      {
        outRequest = sline.IntegrationCore.OutRequests.Get(answer.Id);
      }
      catch (Exception exc)
      {
        var message = sline.IntegrationCore.Resources.OutRequest_EntityNotFoundFormat(answer.Id);
        syncResult.Result = Constants.Module.SyncResult.Error;
        AddMessage(syncResult, Constants.Module.MessageType.Critical, message);

        Logger.WithLogger("IntegrationCore").Error($"UpdateOutRequest > Message: {message}");
      }
      
      if (outRequest != null)
      {
        var answerRow = outRequest.Answers.AddNew();
        answerRow.SystemName = answer.SystemName;
        answerRow.Status = answer.Status;
        answerRow.StatusCode = answer.StatusCode;
        answerRow.Answer = answer.Answer;
        if (answer.DateTime == null)
          answerRow.AnswerDate = Calendar.Now;
        else
          answerRow.AnswerDate = answer.DateTime;
        
        try
        {
          if (outRequest.State.IsChanged)
            outRequest.Save();
        }
        catch (Exception exc)
        {
          var message = exc.Message;
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);

          Logger.WithLogger("IntegrationCore").Error($"UpdateOutRequest > Message: {message}, StackTrace: {exc.StackTrace}");
        }
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"UpdateOutRequest > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("UpdateOutRequest > Completed");
      }
      
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        CreateIncomingRequest(sline.IntegrationCore.Resources.UpdateOutRequestNameFormat(answer.Id, date), outRequest?.Id, 
                              outRequest?.GetType().GetFinalType().GetTypeGuid().ToString(), incomingString, answerString);
      }
      
      return syncResult;
    }
    
    #endregion
    
    #region Входящие запросы
    
    /// <summary>
    /// Создание входящего запроса без сохранения
    /// </summary>
    public virtual IIncRequest CreateIncomingRequest()
    {
      return IncRequests.Create();
    }
    /// <summary>
    /// Создание входящего запроса на основании параметров метода для сущностей без внешних кодов
    /// </summary>
    public virtual IIncRequest CreateIncomingRequest(string name, long? id, string entityType, string body, string answer)
    {
      var request = IncRequests.Create();
      
      request.Name = name;
      request.EntityType = entityType;
      request.EntityId = id;
      
      if (body.Length > 1500)
        request.LargeBody = CreateLargeBody(request, body);
      else
        request.Body = body;
      
      request.Answer = answer;
      request.Save();
      
      return request;
    }
    /// <summary>
    /// Создание входящего запроса на основании структуры (с сохранением)
    /// </summary>
    public virtual IIncRequest CreateIncomingRequest(Structures.Module.IIncomingRequestDto incRequestDto)
    {
      var request = IncRequests.Create();
      
      request.Name = incRequestDto.Name;
      request.EntityType = incRequestDto.EntityType;
      request.EntityId = incRequestDto.EntityId;
      request.ExternalId = incRequestDto.ExternalId;
      request.ExtSystemId = incRequestDto.ExtSystemId;
      
      if (incRequestDto.Body.Length > 1500)
        request.LargeBody = CreateLargeBody(request, incRequestDto.Body);
      else
        request.Body = incRequestDto.Body;
      
      request.Answer = incRequestDto.Answer;
      request.Save();
      
      return request;
    }
    /// <summary>
    /// Создание входящего запроса на основании параметров метода (с сохранением)
    /// </summary>
    public virtual IIncRequest CreateIncomingRequest(string name, string entityType, sline.IntegrationCore.Structures.Module.IEntityModel entityModel, string body, string answer)
    {
      var request = IncRequests.Create();
      
      request.Name = name;
      request.EntityType = entityType;
      request.EntityId = entityModel?.Id;
      request.ExternalId = entityModel?.ExternalId;
      request.ExtSystemId = entityModel?.ExtSystemId;
      
      if (body.Length > 1500)
        request.LargeBody = CreateLargeBody(request, body);
      else
        request.Body = body;
      
      request.Answer = answer;
      request.Save();
      
      return request;
    }
    
    
    #endregion
    
    #region Организационно-штатная структура
    
    #region Должность
    
    /// <summary>
    /// Синхронизация должности
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncJobTitle(Structures.Module.IJobTitleModel jobTitleModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(jobTitleModel);
      var now = Calendar.Now;
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Request: '{incomingString}'");
      }
      
      var findModel = jobTitleModel.Entity;
      var syncResult = CreateResult(findModel);
      var jobTitle = CreateOrUpdateJobTitle(jobTitleModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        syncResult.Hyperlink = Hyperlinks.Get(jobTitle);
        syncResult.Entity.Id = jobTitle?.Id;
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Completed");
      }
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(Sungero.Company.Resources.Jobtitle, date),
                              jobTitle?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
      }
      
      return syncResult;
    }
    /// <summary>
    /// Создание/обновление должности
    /// </summary>
    public virtual IJobTitle CreateOrUpdateJobTitle(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IJobTitle jobTitle = GetOrCreateJobTitle(jobTitleModel, syncResult, isDebug);
      SetJobTitleProperties(jobTitleModel, syncResult, jobTitle, isDebug);
      SaveJobTitle(jobTitleModel, syncResult, jobTitle, isDebug);
      
      return jobTitle;
    }
    /// <summary>
    /// Получение или создание должности
    /// </summary>
    public virtual IJobTitle GetOrCreateJobTitle(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IJobTitle jobTitle = null;
      var findModel = jobTitleModel.Entity;
      
      if (findModel.Id != null && findModel.Id.HasValue)
        jobTitle = JobTitles.GetAll(j => Equals(j.Id, findModel.Id.Value)).SingleOrDefault();
      if (jobTitle == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        jobTitle = JobTitles.GetAll(j => Equals(j.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (jobTitle == null)
        jobTitle = JobTitles.Create();
      
      return jobTitle;
    }
    /// <summary>
    /// Заполнение свойств должности
    /// </summary>
    public virtual void SetJobTitleProperties(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, IJobTitle jobTitle, bool isDebug)
    {
      if (jobTitle == null)
        return;
      
      var newStatus = ProcessEntityStatus(jobTitleModel.Status);
      if (jobTitle.Status != newStatus)
        jobTitle.Status = newStatus;
      
      if (jobTitle.Name != jobTitleModel.Name)
        jobTitle.Name = jobTitleModel.Name;
      
      if (jobTitle.ExternalId != jobTitleModel.Entity.ExternalId)
        jobTitle.ExternalId = jobTitleModel.Entity.ExternalId;
      
      ProcessJobTitleDepartment(jobTitleModel, syncResult, jobTitle, isDebug);
      ProcessObjectExtension(jobTitleModel, syncResult, jobTitle, isDebug);
    }
    /// <summary>
    /// Обработка подразделения для должности
    /// </summary>
    public virtual void ProcessJobTitleDepartment(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, IJobTitle jobTitle, bool isDebug)
    {
      var department = GetJobTitleDepartment(jobTitleModel, syncResult, isDebug);
      if (department == null && jobTitleModel.Department != null)
      {
        var message = sline.IntegrationCore.Resources.JobTitle_DepartmentNotFoundFormat(jobTitleModel.Department?.Id, jobTitleModel.Department?.ExternalId,
                                                                                        jobTitleModel.Entity?.Id, jobTitleModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > GetJobTitleDepartment > Message: {message}");
      }
      if (jobTitle.Department != department)
        jobTitle.Department = department;
    }
    /// <summary>
    /// Получение подразделения для должности
    /// </summary>
    public virtual IDepartment GetJobTitleDepartment(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IDepartment department = null;
      
      var findModel = jobTitleModel.Department;
      if (findModel == null)
        return department;
      if (findModel.Id != null && findModel.Id.HasValue)
        department = Departments.GetAll(d => Equals(d.Id, findModel.Id.Value)).SingleOrDefault();
      if (department == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        department = Departments.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return department;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, IJobTitle jobTitle, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранение должности
    /// </summary>
    public virtual void SaveJobTitle(Structures.Module.IJobTitleModel jobTitleModel, Structures.Module.ISyncResult syncResult, IJobTitle jobTitle, bool isDebug)
    {
      if (jobTitle == null)
        return;
      
      if (jobTitle.State.IsChanged)
      {
        try
        {
          jobTitle.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(jobTitleModel.Entity?.Id, jobTitleModel.Entity?.ExternalId, validationErrors);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateJobTitle > Error: {message}, StackTrace: {vExc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(jobTitleModel.Entity?.Id, jobTitleModel.Entity?.ExternalId, exc.Message);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateJobTitle > Error: {message}, StackTrace: {exc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
      }
    }
    
    #endregion
    
    #region Персона
    
    /// <summary>
    /// Синхронизация персоны
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncPerson(Structures.Module.IPersonModel personModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(personModel);
      var now = Calendar.Now;
      
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncPerson > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncPerson > Request: '{incomingString}'");
      }
      
      var findModel = personModel.Entity;
      var syncResult = CreateResult(findModel);
      var person = CreateOrUpdatePerson(personModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        syncResult.Hyperlink = Hyperlinks.Get(person);
        syncResult.Entity.Id = person?.Id;
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"SyncPerson > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("SyncPerson > Completed");
      }
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(sline.IntegrationCore.Resources.PersonName, date),
                              person?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
      }
      
      return syncResult;
    }
    /// <summary>
    /// Создание/обновление персоны
    /// </summary>
    public virtual IPerson CreateOrUpdatePerson(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IPerson person = GetOrCreatePerson(personModel, syncResult, isDebug);
      SetPersonProperties(personModel, syncResult, person, isDebug);
      SavePerson(personModel, syncResult, person, isDebug);
      
      return person;
    }
    /// <summary>
    /// Получить или создать персону
    /// </summary>
    public virtual IPerson GetOrCreatePerson(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IPerson person = null;
      var findModel = personModel.Entity;
      
      if (findModel.Id != null && findModel.Id.HasValue)
        person = People.GetAll(p => Equals(p.Id, findModel.Id.Value)).SingleOrDefault();
      if (person == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        person = People.GetAll(p => Equals(p.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (person == null)
        person = People.Create();
      
      return person;
    }
    /// <summary>
    /// Заполнение свойств персоны
    /// </summary>
    public virtual void SetPersonProperties(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, IPerson person, bool isDebug)
    {
      if (person == null)
        return;
      
      var newStatus = ProcessEntityStatus(personModel.Status);
      if (person.Status != newStatus)
        person.Status = newStatus;
      
      if (person.FirstName != personModel.FirstName)
        person.FirstName = personModel.FirstName;
      
      if (person.LastName != personModel.LastName)
        person.LastName = personModel.LastName;
      
      if (person.MiddleName != personModel.MiddleName)
        person.MiddleName = personModel.MiddleName;
      
      if (person.ExternalId != personModel.Entity?.ExternalId)
        person.ExternalId = personModel.Entity?.ExternalId;
      
      var sex = ProcessPersonSex(personModel.Sex);
      if (person.Sex != sex)
        person.Sex = sex;

      if (person.DateOfBirth != personModel.DateOfBirth)
        person.DateOfBirth = personModel.DateOfBirth;
      
      ProcessPersonTIN(personModel, syncResult, person, isDebug);
      
      if (person.INILA != personModel.INILA)
        person.INILA = personModel.INILA;
      
      ProcessObjectExtension(personModel, syncResult, person, isDebug);
    }
    /// <summary>
    /// Обработка ИНН персоны из ОШС
    /// </summary>
    public virtual void ProcessPersonTIN(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, IPerson person, bool isDebug)
    {
      var message = CheckTIN(personModel.TIN, false);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessPersonTIN > Message: {message}");
      }
      else if (person.TIN != personModel.TIN)
        person.TIN = personModel.TIN;
    }
    /// <summary>
    /// Обработка пола персоны
    /// </summary>
    public virtual Enumeration ProcessPersonSex(string extSex)
    {
      var sex = Sungero.Parties.Person.Sex.Male;
      switch (extSex)
      {
        case "Male":
          sex = Sungero.Parties.Person.Sex.Male;
          break;
        case "Female":
          sex = Sungero.Parties.Person.Sex.Female;
          break;
        default:
          break;
      }
      
      return sex;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, IPerson person, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранение персоны
    /// </summary>
    public virtual void SavePerson(Structures.Module.IPersonModel personModel, Structures.Module.ISyncResult syncResult, IPerson person, bool isDebug)
    {
      if (person == null)
        return;
      
      if (person.State.IsChanged)
      {
        try
        {
          person.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(personModel.Entity?.Id, personModel.Entity?.ExternalId, validationErrors);
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
          
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdatePerson > Message: {message}, StackTrace: {vExc.StackTrace}");
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(personModel.Entity?.Id, personModel.Entity?.ExternalId, exc.Message);
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
          
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdatePerson > Message: {message}, StackTrace: {exc.StackTrace}");
        }
      }
    }
    
    #endregion
    
    #region Подразделение
    
    /// <summary>
    /// Синхронизация подразделения
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncDepartment(Structures.Module.IDepartmentModel departmentModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(departmentModel);
      var now = Calendar.Now;
      
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncDepartment > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncDepartment > Request: '{incomingString}'");
      }
      
      var findModel = departmentModel.Entity;
      var syncResult = CreateResult(findModel);
      var department = CreateOrUpdateDepartment(departmentModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        syncResult.Hyperlink = Hyperlinks.Get(department);
        syncResult.Entity.Id = department?.Id;
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"SyncDepartment > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("SyncDepartment > Completed");
      }
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(sline.IntegrationCore.Resources.DepartmentName, date),
                              department?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
      }
      
      return syncResult;
    }
    /// <summary>
    /// Создание/обновление подразделения
    /// </summary>
    public virtual IDepartment CreateOrUpdateDepartment(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IDepartment department = GetOrCreateDepartment(departmentModel, syncResult, isDebug);
      SetDepartmentProperties(departmentModel, syncResult, department, isDebug);
      SaveDepartment(departmentModel, syncResult, department, isDebug);
      
      return department;
    }
    /// <summary>
    /// Получение или создание подразделения
    /// </summary>
    public virtual IDepartment GetOrCreateDepartment(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IDepartment department = null;
      
      var findModel = departmentModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        department = Departments.GetAll(d => Equals(d.Id, findModel.Id.Value)).SingleOrDefault();
      if (department == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        department = Departments.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (department == null)
        department = Departments.Create();
      
      return department;
    }
    /// <summary>
    /// Заполнение свойств подразделения
    /// </summary>
    public virtual void SetDepartmentProperties(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      if (department == null)
        return;
      
      var newStatus = ProcessEntityStatus(departmentModel.Status);
      if (department.Status != newStatus)
        department.Status = newStatus;
      
      if (department.Name != departmentModel.Name)
        department.Name = departmentModel.Name;
      
      if (department.ExternalId != departmentModel.Entity?.ExternalId)
        department.ExternalId = departmentModel.Entity?.ExternalId;
      
      ProcessHeadOffice(departmentModel, syncResult, department, isDebug);
      ProcessBusinessUnit(departmentModel, syncResult, department, isDebug);
      ProcessManager(departmentModel, syncResult, department, isDebug);
      ProcessObjectExtension(departmentModel, syncResult, department, isDebug);
    }
    /// <summary>
    /// Обработка вышестоящего подразделения
    /// </summary>
    public virtual void ProcessHeadOffice(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      var headOffice = GetHeadOffice(departmentModel, syncResult, isDebug);
      
      if (headOffice == null && departmentModel.HeadOffice != null)
      {
        var message = sline.IntegrationCore.Resources.Departments_HeadNotFoundFormat(departmentModel.HeadOffice?.Id, departmentModel.HeadOffice?.ExternalId,
                                                                                     departmentModel.Entity?.Id, departmentModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessHeadOffice > Message: {message}");
      }
      
      if (department.HeadOffice != headOffice)
        department.HeadOffice = headOffice;
    }
    /// <summary>
    /// Получение вышестоящего подразделения
    /// </summary>
    public virtual IDepartment GetHeadOffice(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      // обработка вышестоящего подразделения
      IDepartment headOffice = null;
      
      var findModel = departmentModel.HeadOffice;
      if (findModel == null)
        return headOffice;
      if (findModel.Id != null && findModel.Id.HasValue)
        headOffice = Departments.GetAll(d => Equals(d.Id, findModel.Id.Value)).SingleOrDefault();
      if (headOffice == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        headOffice = Departments.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return headOffice;
    }
    /// <summary>
    /// Обработка НОР для подразделения
    /// </summary>
    public virtual void ProcessBusinessUnit(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      var businessUnit = GetBusinessUnit(departmentModel, syncResult, isDebug);
      
      if (businessUnit == null && departmentModel.BusinessUnit != null)
      {
        var message = sline.IntegrationCore.Resources.Department_BusinessUnitNotFoundFormat(departmentModel.BusinessUnit?.Id, departmentModel.BusinessUnit?.ExternalId,
                                                                                            departmentModel.Entity?.Id, departmentModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessBusinessUnit > Message: {message}");
      }
      if (department.BusinessUnit != businessUnit)
        department.BusinessUnit = businessUnit;
    }
    /// <summary>
    /// Получение НОР для подразделения
    /// </summary>
    public virtual IBusinessUnit GetBusinessUnit(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      // обработка НОР
      IBusinessUnit businessUnit = null;
      
      var findModel = departmentModel.BusinessUnit;
      if (findModel == null)
        return businessUnit;
      if (findModel.Id != null && findModel.Id.HasValue)
        businessUnit = BusinessUnits.GetAll(b => Equals(b.Id, findModel.Id.Value)).SingleOrDefault();
      if (businessUnit == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        businessUnit = BusinessUnits.GetAll(b => Equals(b.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return businessUnit;
    }
    /// <summary>
    /// Обработка руководителя для подразделения
    /// </summary>
    public virtual void ProcessManager(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      var manager = GetManager(departmentModel, syncResult, isDebug);
      
      if (manager == null && departmentModel.Manager != null)
      {
        var message = sline.IntegrationCore.Resources.Department_ManagerNotFoundFormat(departmentModel.Manager?.Id, departmentModel.Manager?.ExternalId, departmentModel.Entity?.Id, departmentModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessManager > Message: {message}");
      }
      if (department.Manager != manager)
        department.Manager = manager;
    }
    /// <summary>
    /// Получение руководителя для подразделения
    /// </summary>
    public virtual IEmployee GetManager(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      // обработка руководителя
      IEmployee manager = null;
      
      var findModel = departmentModel.Manager;
      if (findModel == null)
        return manager;
      if (findModel.Id != null && findModel.Id.HasValue)
        manager = Employees.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (manager == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        manager = Employees.GetAll(e => Equals(e.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return manager;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранение подразделения
    /// </summary>
    public virtual void SaveDepartment(Structures.Module.IDepartmentModel departmentModel, Structures.Module.ISyncResult syncResult, IDepartment department, bool isDebug)
    {
      if (department == null)
        return;
      
      if (department.State.IsChanged)
      {
        try
        {
          department.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(departmentModel.Entity?.Id, departmentModel.Entity?.ExternalId, validationErrors);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateDepartment > Message: {message}, StackTrace: {vExc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(departmentModel.Entity?.Id, departmentModel.Entity?.ExternalId, exc.Message);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateDepartment > Message: {message}, StackTrace: {exc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
      }
    }
    
    #endregion

    #region Наша организация
    
    /// <summary>
    /// Синхронизация НОР
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(businessUnitModel);
      var now = Calendar.Now;
      
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncBusinessUnit > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncBusinessUnit > Request: '{incomingString}'");
      }
      
      var findModel = businessUnitModel.Entity;
      var syncResult = CreateResult(findModel);
      var businessUnit = CreateOrUpdateBusinessUnit(businessUnitModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        syncResult.Hyperlink = Hyperlinks.Get(businessUnit);
        syncResult.Entity.Id = businessUnit?.Id;
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"SyncBusinessUnit > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("SyncBusinessUnit > Completed");
      }
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        
        Logger.WithLogger("IntegrationCore").Debug($"{businessUnit?.GetType().GetFinalType().GUID.ToString()}");
        Logger.WithLogger("IntegrationCore").Debug($"{businessUnit?.GetType().GetFinalType().GUID.ToString()}");
        
        CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(sline.IntegrationCore.Resources.BusinessUnitName, date),
                              businessUnit?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
      }
      
      return syncResult;
    }
    /// <summary>
    /// Создание/обновление НОР
    /// </summary>
    public virtual IBusinessUnit CreateOrUpdateBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IBusinessUnit businessUnit = GetOrCreateBusinessUnit(businessUnitModel, syncResult, isDebug);
      SetBusinessUnitProperties(businessUnitModel, syncResult, businessUnit, isDebug);
      SaveBusinessUnit(businessUnitModel, syncResult, businessUnit, isDebug);
      
      return businessUnit;
    }
    /// <summary>
    /// Получение или создание НОР
    /// </summary>
    public virtual IBusinessUnit GetOrCreateBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IBusinessUnit businessUnit = null;
      
      var findModel = businessUnitModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        businessUnit = BusinessUnits.GetAll(b => Equals(b.Id, findModel.Id.Value)).SingleOrDefault();
      if (businessUnit == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        businessUnit = BusinessUnits.GetAll(b => Equals(b.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (businessUnit == null)
        businessUnit = BusinessUnits.Create();
      
      return businessUnit;
    }
    /// <summary>
    /// Заполнение свойств НОР
    /// </summary>
    public virtual void SetBusinessUnitProperties(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      if (businessUnit == null)
        return;
      
      var newStatus = ProcessEntityStatus(businessUnitModel.Status);
      if (businessUnit.Status != newStatus)
        businessUnit.Status = newStatus;
      
      if (businessUnit.Name != businessUnitModel.Name)
        businessUnit.Name = businessUnitModel.Name;
      
      if (businessUnit.LegalName != businessUnitModel.LegalName)
        businessUnit.LegalName = businessUnitModel.LegalName;
      
      if (businessUnit.ExternalId != businessUnitModel.Entity?.ExternalId)
        businessUnit.ExternalId = businessUnitModel.Entity?.ExternalId;
      
      ProcessTIN(businessUnitModel, syncResult, businessUnit, isDebug);
      ProcessTRRC(businessUnitModel, syncResult, businessUnit, isDebug);
      
      if (businessUnit.PSRN != businessUnitModel.PSRN)
        businessUnit.PSRN = businessUnitModel.PSRN;
      
      ProcessHeadBusinessUnit(businessUnitModel, syncResult, businessUnit, isDebug);
      ProcessCEO(businessUnitModel, syncResult, businessUnit, isDebug);
      ProcessObjectExtension(businessUnitModel, syncResult, businessUnit, isDebug);
    }
    /// <summary>
    /// Обработка руководителя НОР
    /// </summary>
    public virtual void ProcessCEO(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      var CEO = GetCEO(businessUnitModel, syncResult, isDebug);
      
      if (CEO == null && businessUnitModel.CEO != null)
      {
        var message = sline.IntegrationCore.Resources.BusinessUnit_CEONotFoundFormat(businessUnitModel.CEO?.Id, businessUnitModel.CEO?.ExternalId,
                                                                                     businessUnitModel.Entity?.Id, businessUnitModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"SyncBusinessUnit > GetCEO > Message: {message}");
      }
      if (businessUnit.CEO != CEO)
        businessUnit.CEO = CEO;
    }
    /// <summary>
    /// Получение руководителя НОР
    /// </summary>
    public virtual IEmployee GetCEO(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IEmployee CEO = null;
      
      var findModel = businessUnitModel.CEO;
      if (findModel == null)
        return CEO;
      if (findModel.Id != null && findModel.Id.HasValue)
        CEO = Employees.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (CEO == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        CEO = Employees.GetAll(e => Equals(e.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return CEO;
    }
    /// <summary>
    /// Обработка вышестоящей НОР
    /// </summary>
    public virtual void ProcessHeadBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      var headCompany = GetHeadBusinessUnit(businessUnitModel, syncResult, isDebug);
      
      if (headCompany == null && businessUnitModel.HeadCompany != null)
      {
        var message = sline.IntegrationCore.Resources.BusinessUnit_HeadCompanyNotFoundFormat(businessUnitModel.HeadCompany?.Id, businessUnitModel.HeadCompany?.ExternalId,
                                                                                             businessUnitModel.Entity?.Id, businessUnitModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"SyncBusinessUnit > GetBusinessUnit > Message: {message}");
      }
      if (businessUnit.HeadCompany != headCompany)
        businessUnit.HeadCompany = headCompany;
    }
    /// <summary>
    /// Получение вышестоящей НОР
    /// </summary>
    public virtual IBusinessUnit GetHeadBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IBusinessUnit headCompany = null;
      
      var findModel = businessUnitModel.HeadCompany;
      if (findModel == null)
        return headCompany;
      if (findModel.Id != null && findModel.Id.HasValue)
        headCompany = BusinessUnits.GetAll(b => Equals(b.Id, findModel.Id.Value)).SingleOrDefault();
      if (headCompany == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        headCompany = BusinessUnits.GetAll(b => Equals(b.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return headCompany;
    }
    /// <summary>
    /// Обработка ИНН
    /// </summary>
    public virtual void ProcessTIN(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      var errorMessage = this.CheckTIN(businessUnitModel.TIN, true);
      if (!string.IsNullOrEmpty(errorMessage))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, errorMessage);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"SetBusinessUnitProperties > CheckTin > Message: {errorMessage}");
      }
      else if (businessUnit.TIN != businessUnitModel.TIN)
        businessUnit.TIN = businessUnitModel.TIN;
    }
    /// <summary>
    /// Обработка КПП
    /// </summary>
    public virtual void ProcessTRRC(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      var errorMessage = this.CheckTRRC(businessUnitModel.TRRC);
      if (!string.IsNullOrEmpty(errorMessage))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, errorMessage);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"SetBusinessUnitProperties > CheckTRRC > Message: {errorMessage}");
      }
      else if (businessUnit.TRRC != businessUnitModel.TRRC)
        businessUnit.TRRC = businessUnitModel.TRRC;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранение НОР
    /// </summary>
    public virtual void SaveBusinessUnit(Structures.Module.IBusinessUnitModel businessUnitModel, Structures.Module.ISyncResult syncResult, IBusinessUnit businessUnit, bool isDebug)
    {
      if (businessUnit == null)
        return;
      
      if (businessUnit.State.IsChanged)
      {
        try
        {
          businessUnit.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(businessUnitModel.Entity?.Id, businessUnitModel.Entity?.ExternalId, validationErrors);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateBusinessUnit > Message: {message}, StackTrace: {vExc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(businessUnitModel.Entity?.Id, businessUnitModel.Entity?.ExternalId, exc.Message);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateBusinessUnit > Message: {message}, StackTrace: {exc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
      }
    }
    
    #endregion
    
    #region Сотрудник
    
    /// <summary>
    /// Синхронизирует сотрудника из внешней системы.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncEmployee(Structures.Module.IEmployeeModel employeeModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(employeeModel);
      var now = Calendar.Now;
      
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncEmployee > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncEmployee > Request: '{incomingString}'");
      }
      
      var findModel = employeeModel.Entity;
      var syncResult = CreateResult(findModel);
      IEmployee employee = CreateOrUpdateEmployee(employeeModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        syncResult.Hyperlink = Hyperlinks.Get(employee);
        syncResult.Entity.Id = employee?.Id;
      }
      
      var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug($"SyncEmployee > Answer: '{answerString}'");
        Logger.WithLogger("IntegrationCore").Debug("SyncEmployee > Completed");
      }
      if (captureRequest)
      {
        string date = now.ToString("dd.MM.yyyy HH:mm:ss");
        CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(Sungero.Company.Resources.Employee, date),
                              employee?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
      }
      
      return syncResult;
    }
    /// <summary>
    /// Создает/обновляет сущность сотрудника
    /// </summary>
    public virtual IEmployee CreateOrUpdateEmployee(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IEmployee employee = GetOrCreateEmployee(employeeModel, syncResult, isDebug);
      SetEmployeeProperties(employeeModel, syncResult, employee, isDebug);
      SaveEmployee(employeeModel, syncResult, employee, isDebug);
      
      return employee;
    }
    /// <summary>
    /// Получение или создание сотрудника.
    /// </summary>
    public virtual IEmployee GetOrCreateEmployee(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IEmployee employee = null;
      
      var findModel = employeeModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        employee = Employees.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (employee == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        employee = Employees.GetAll(e => Equals(e.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (employee == null)
        employee = Employees.Create();
      
      return employee;
    }
    /// <summary>
    /// Заполнение свойств сотрудника
    /// </summary>
    public virtual void SetEmployeeProperties(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      if (employee == null)
        return;
      
      var newStatus = ProcessEntityStatus(employeeModel.Status);
      if (employee.Status != newStatus)
        employee.Status = newStatus;
      
      ProcessPerson(employeeModel, syncResult, employee, isDebug);
      ProcessDepartment(employeeModel, syncResult, employee, isDebug);
      ProcessJobTitle(employeeModel, syncResult, employee, isDebug);
      
      if (employee.Phone != employeeModel.Phone)
        employee.Phone = employeeModel.Phone;
      
      if (employee.PersonnelNumber != employeeModel.PersonnelNumber)
        employee.PersonnelNumber = employeeModel.PersonnelNumber;
      
      if (employee.ExternalId != employeeModel.Entity?.ExternalId)
        employee.ExternalId = employeeModel.Entity?.ExternalId;
      
      ProcessNotifications(employeeModel, syncResult, employee, isDebug);
      ProcessEmail(employeeModel, syncResult, employee, isDebug);
      ProcessObjectExtension(employeeModel, syncResult, employee, isDebug);
    }
    /// <summary>
    /// Обработка персоны сотрудника
    /// </summary>
    public void ProcessPerson(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      IPerson person = GetPerson(employeeModel, syncResult, isDebug);
      if (person == null)
      {
        var message = sline.IntegrationCore.Resources.Employee_PersonNotFoundFormat(employeeModel.Person?.Id, employeeModel.Person?.ExternalId,
                                                                                    employeeModel.Entity?.Id, employeeModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Error;
        AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        Logger.WithLogger("IntegrationCore").Error($"ProcessPerson > Message: {message}");
      }
      else if (employee.Person != person)
        employee.Person = person;
    }
    /// <summary>
    /// Обработка подразделения сотрудника
    /// </summary>
    public void ProcessDepartment(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      IDepartment department = GetDepartment(employeeModel, syncResult, isDebug);
      if (department == null)
      {
        var message = sline.IntegrationCore.Resources.Employee_DepartmentNotFoundFormat(employeeModel.Department?.Id, employeeModel.Department?.ExternalId,
                                                                                        employeeModel.Entity?.Id, employeeModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Error;
        AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        Logger.WithLogger("IntegrationCore").Error($"ProcessDepartment > Message: {message}");
      }
      else if (employee.Department != department)
        employee.Department = department;
    }
    /// <summary>
    /// Обработка должности сотрудника
    /// </summary>
    public void ProcessJobTitle(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      IJobTitle jobTitle = GetJobTitle(employeeModel, syncResult, isDebug);
      if (jobTitle == null)
      {
        var message = sline.IntegrationCore.Resources.Employee_JobTitleNotFoundFormat(employeeModel.JobTitle?.Id, employeeModel.JobTitle?.ExternalId,
                                                                                      employeeModel.Entity?.Id, employeeModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessJobTitle > Message: {message}");
      }
      if (employee.JobTitle != jobTitle)
        employee.JobTitle = jobTitle;
    }
    /// <summary>
    /// Получение должности для сотрудника
    /// </summary>
    public virtual IJobTitle GetJobTitle(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IJobTitle jobTitle = null;
      
      var findModel = employeeModel.JobTitle;
      if (findModel == null)
        return jobTitle;
      if (findModel.Id != null && findModel.Id.HasValue)
        jobTitle = JobTitles.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (jobTitle == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        jobTitle = JobTitles.GetAll(j => Equals(j.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return jobTitle;
    }
    /// <summary>
    /// Получение персоны для сотрудника
    /// </summary>
    public virtual IPerson GetPerson(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IPerson person = null;
      
      var findModel = employeeModel.Person;
      if (findModel == null)
        return person;
      if (findModel.Id != null && findModel.Id.HasValue)
        person = People.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (person == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        person = People.GetAll(p => Equals(p.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return person;
    }
    /// <summary>
    /// Получение подразделения для сотрудника
    /// </summary>
    public virtual IDepartment GetDepartment(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IDepartment department = null;
      
      var findModel = employeeModel.Department;
      if (findModel == null)
        return department;
      if (findModel.Id != null && findModel.Id.HasValue)
        department = Departments.GetAll(e => Equals(e.Id, findModel.Id.Value)).SingleOrDefault();
      if (department == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        department = Departments.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return department;
    }
    /// <summary>
    /// Проверка почтового адреса для сотрудника
    /// </summary>
    public virtual void ProcessEmail(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      if (!string.IsNullOrWhiteSpace(employeeModel.Email))
      {
        var emailError = string.Empty;
        
        if (!Sungero.Parties.PublicFunctions.Module.EmailIsValid(employeeModel.Email))
          emailError = Sungero.Parties.Resources.WrongEmailFormat;
        
        else if (!Sungero.Docflow.PublicFunctions.Module.IsASCII(employeeModel.Email))
          emailError = Sungero.Docflow.Resources.ASCIIWarning;
        
        if (string.IsNullOrWhiteSpace(emailError))
        {
          if (employee.Email != employeeModel.Email)
            employee.Email = employeeModel.Email;
        }
        else
        {
          syncResult.Result = Constants.Module.SyncResult.Warning;
          AddMessage(syncResult, Constants.Module.MessageType.Info, emailError);
          if (isDebug)
            Logger.WithLogger("IntegrationCore").Debug($"SyncEmployee > ProcessEmail > Message: {emailError}");
        }
      }
      else
        employee.Email = null;
    }
    /// <summary>
    /// Проверка признаков уведомлений
    /// </summary>
    public virtual void ProcessNotifications(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      bool notify = employeeModel.NeedNotifyExpiredAssignments.GetValueOrDefault();
      if (employee.NeedNotifyExpiredAssignments != notify)
        employee.NeedNotifyExpiredAssignments = notify;
      
      notify = employeeModel.NeedNotifyNewAssignments.GetValueOrDefault();
      if (employee.NeedNotifyNewAssignments != notify)
        employee.NeedNotifyNewAssignments = notify;
      
      notify = employeeModel.NeedNotifyAssignmentsSummary.GetValueOrDefault();
      if (employee.NeedNotifyAssignmentsSummary != notify)
        employee.NeedNotifyAssignmentsSummary = notify;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранение сотрудника
    /// </summary>
    public virtual void SaveEmployee(Structures.Module.IEmployeeModel employeeModel, Structures.Module.ISyncResult syncResult, IEmployee employee, bool isDebug)
    {
      if (employee == null)
        return;
      
      if (employee.State.IsChanged)
      {
        try
        {
          employee.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(employeeModel.Entity?.Id, employeeModel.Entity?.ExternalId, validationErrors);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateEmployee > Message: {message}, StackTrace: {vExc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(employeeModel.Entity?.Id, employeeModel.Entity?.ExternalId, exc.Message);
          Logger.WithLogger("IntegrationCore").Error($"CreateUpdateEmployee > Message: {message}, StackTrace: {exc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
      }
    }
    
    #endregion
    
    #endregion
    
    #region Контрагенты
    
    /// <summary>
    /// Синхронизация контрагента
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual Structures.Module.ISyncResult SyncCounterparty(Structures.Module.ICounterpartyModel counterpartyModel)
    {
      var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(counterpartyModel);
      var now = Calendar.Now;
      
      bool isDebug = GetDebugModeSetting();
      bool captureRequest = GetIncomingRequestSetting();
      
      if (isDebug)
      {
        Logger.WithLogger("IntegrationCore").Debug("SyncCounterparty > Start");
        Logger.WithLogger("IntegrationCore").Debug($"SyncCounterparty > Request: '{incomingString}'");
      }
      
      var findModel = counterpartyModel.Entity;
      var syncResult = CreateResult(findModel);
      CheckRequiredValues(counterpartyModel, syncResult, isDebug);
      
      if (syncResult.Result != Constants.Module.SyncResult.Error)
      {
        var counterparty = CreateOrUpdateCounterparty(counterpartyModel, syncResult, isDebug);
        if (syncResult.Result != Constants.Module.SyncResult.Error)
        {
          syncResult.Hyperlink = Hyperlinks.Get(counterparty);
          syncResult.Entity.Id = counterparty?.Id;
        }
        
        var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
        if (isDebug)
        {
          Logger.WithLogger("IntegrationCore").Debug($"SyncCounterparty > Answer: '{answerString}'");
          Logger.WithLogger("IntegrationCore").Debug("SyncCounterparty > Completed");
        }
        if (captureRequest)
        {
          string date = now.ToString("dd.MM.yyyy HH:mm:ss");
          CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(Sungero.Parties.Resources.Counterparty, date),
                                counterparty?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
        }
      }

      return syncResult;
    }
    /// <summary>
    /// Проверка обязательных свойств
    /// </summary>
    public virtual void CheckRequiredValues(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      if (string.IsNullOrWhiteSpace(counterpartyModel.EntityType))
      {
        var message = sline.IntegrationCore.Resources.Counterparty_RequiredFieldsErrorFormat("EntityType");
        Logger.WithLogger("IntegrationCore").Error($"CreateOrUpdateCounterparty > CheckRequiredValues > Message: {message}");
        syncResult.Result = Constants.Module.SyncResult.Error;
        AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
      }
    }
    /// <summary>
    /// Создание/обновление контрагента
    /// </summary>
    public virtual ICounterparty CreateOrUpdateCounterparty(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      ICounterparty counterparty = null;

      if (counterpartyModel.EntityType == Constants.Module.CounterpartyType.Company)
        counterparty = GetOrCreateCompany(counterpartyModel, syncResult);
      
      if (counterpartyModel.EntityType == Constants.Module.CounterpartyType.Bank)
        counterparty = GetOrCreateBank(counterpartyModel, syncResult);
      
      if (counterpartyModel.EntityType == Constants.Module.CounterpartyType.Person)
        counterparty = GetOrCreatePerson(counterpartyModel, syncResult);
      
      SetCounterpartyProperties(counterpartyModel, syncResult, counterparty, isDebug);
      SaveCounterparty(counterpartyModel, syncResult, counterparty);
      
      return counterparty;
    }
    /// <summary>
    /// Получить/создать организацию-контрагента
    /// </summary>
    public virtual ICompany GetOrCreateCompany(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult)
    {
      ICompany company = null;
      
      var findModel = counterpartyModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        company = Companies.GetAll(c => Equals(c.Id, findModel.Id.Value)).SingleOrDefault();
      if (company == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        company = Companies.GetAll(c => Equals(c.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (company == null)
        company = Companies.Create();
      
      return company;
    }
    /// <summary>
    /// Получить/создать банк-контрагента
    /// </summary>
    public virtual IBank GetOrCreateBank(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult)
    {
      IBank bank = null;
      
      var findModel = counterpartyModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        bank = Banks.GetAll(c => Equals(c.Id, findModel.Id.Value)).SingleOrDefault();
      if (bank == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        bank = Banks.GetAll(c => Equals(c.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (bank == null)
        bank = Banks.Create();
      
      return bank;
    }
    /// <summary>
    /// Получить/создать персону-контрагента
    /// </summary>
    public virtual IPerson GetOrCreatePerson(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult)
    {
      IPerson person = null;
      
      var findModel = counterpartyModel.Entity;
      if (findModel.Id != null && findModel.Id.HasValue)
        person = People.GetAll(c => Equals(c.Id, findModel.Id.Value)).SingleOrDefault();
      if (person == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        person = People.GetAll(c => Equals(c.ExternalId, findModel.ExternalId)).SingleOrDefault();
      if (person == null)
        person = People.Create();
      
      return person;
    }
    /// <summary>
    /// Заполнить свойства контрагента
    /// </summary>
    public virtual void SetCounterpartyProperties(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      SetCommonProperties(counterpartyModel, syncResult, counterparty, isDebug);
      
      // организации
      if (Companies.Is(counterparty))
        SetCompanyProperties(counterpartyModel, syncResult, counterparty, isDebug);
      
      // банки
      if (Banks.Is(counterparty))
        SetBankProperties(counterpartyModel, syncResult, counterparty, isDebug);
      
      // персоны
      if (People.Is(counterparty))
        SetPersonProperties(counterpartyModel, syncResult, counterparty, isDebug);
      
      // ссылочные поля
      ProcessResponsible(counterpartyModel, syncResult, counterparty, isDebug);
      ProcessObjectExtension(counterpartyModel, syncResult, counterparty, isDebug);
    }
    /// <summary>
    /// Заполнить свойства организации-контрагента
    /// </summary>
    public virtual void SetCommonProperties(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      if (counterparty.ExternalId != counterpartyModel.Entity?.ExternalId)
        counterparty.ExternalId = counterpartyModel.Entity?.ExternalId;
      
      if (counterparty.Name != counterpartyModel.Name)
        counterparty.Name = counterpartyModel.Name;
      
      if (counterpartyModel.Nonresident != null)
        if (!Equals(counterparty.Nonresident, counterpartyModel.Nonresident))
          counterparty.Nonresident = counterpartyModel.Nonresident;
      
      var newStatus = ProcessEntityStatus(counterpartyModel.Status);
      if (counterparty.Status != newStatus)
        counterparty.Status = newStatus;
      
      if (counterparty.PSRN != counterpartyModel.PSRN)
        counterparty.PSRN = counterpartyModel.PSRN;
      if (counterparty.NCEO != counterpartyModel.NCEO)
        counterparty.NCEO = counterpartyModel.NCEO;
      if (counterparty.NCEA != counterpartyModel.NCEA)
        counterparty.NCEA = counterpartyModel.NCEA;
      if (counterparty.LegalAddress != counterpartyModel.LegalAddress)
        counterparty.LegalAddress = counterpartyModel.LegalAddress;
      if (counterparty.PostalAddress != counterpartyModel.PostalAddress)
        counterparty.PostalAddress = counterpartyModel.PostalAddress;
      if (counterparty.Phones != counterpartyModel.Phones)
        counterparty.Phones = counterpartyModel.Phones;
      if (counterparty.Email != counterpartyModel.Email)
        counterparty.Email = counterpartyModel.Email;
      if (counterparty.Homepage != counterpartyModel.Homepage)
        counterparty.Homepage = counterpartyModel.Homepage;
      if (counterparty.Account != counterpartyModel.Account)
        counterparty.Account = counterpartyModel.Account;
      if (counterparty.Note != counterpartyModel.Note)
        counterparty.Note = counterpartyModel.Note;
      if (counterparty.Code != counterpartyModel.Code)
        counterparty.Code = counterpartyModel.Code;
    }
    /// <summary>
    /// Заполнить свойства организации-контрагента
    /// </summary>
    public virtual void SetCompanyProperties(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var company = Companies.As(counterparty);
      
      if (company.LegalName != counterpartyModel.LegalName)
        company.LegalName = counterpartyModel.LegalName;
      
      ProcessCompanyTIN(counterpartyModel, syncResult, company, isDebug);
      ProcessCompanyTRRC(counterpartyModel, syncResult, company, isDebug);
      ProcessHeadCompany(counterpartyModel, syncResult, company, isDebug);
    }
    /// <summary>
    /// Заполнить свойства банка-контрагента
    /// </summary>
    public virtual void SetBankProperties(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var bank = Banks.As(counterparty);
      
      ProcessCompanyTIN(counterpartyModel, syncResult, counterparty, isDebug);
      ProcessBIC(counterpartyModel, syncResult, bank, isDebug);
      ProcessSWIFT(counterpartyModel, syncResult, bank, isDebug);
      
      if (bank.CorrespondentAccount != counterpartyModel.CorrespondentAccount)
        bank.CorrespondentAccount = counterpartyModel.CorrespondentAccount;
    }
    /// <summary>
    /// Заполнить свойства персоны-контрагента
    /// </summary>
    public virtual void SetPersonProperties(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var person = People.As(counterparty);
      
      ProcessPersonTIN(counterpartyModel, syncResult, counterparty, isDebug);
      
      if (person.LastName != counterpartyModel.LastName)
        person.LastName = counterpartyModel.LastName;
      if (person.FirstName != counterpartyModel.FirstName)
        person.FirstName = counterpartyModel.FirstName;
      if (person.MiddleName != counterpartyModel.MiddleName)
        person.MiddleName = counterpartyModel.MiddleName;
      
      var INILA = Sungero.Parties.PublicFunctions.Person.RemoveInilaSpecialSymbols(counterpartyModel.INILA);
      if (person.INILA != INILA)
        person.INILA = INILA;
      
      if (person.DateOfBirth != counterpartyModel.DateOfBirth)
        person.DateOfBirth = counterpartyModel.DateOfBirth;
      
      var sex = ProcessPersonSex(counterpartyModel.Sex);
      if (person.Sex != sex)
        person.Sex = sex;
    }
    /// <summary>
    /// Обработка ИНН персоны из контрагента
    /// </summary>
    public virtual void ProcessPersonTIN(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var message = CheckTIN(counterpartyModel.TIN, false);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($" >  ProcessPersonTIN > Message: {message}");
      }
      else if (counterparty.TIN != counterpartyModel.TIN)
        counterparty.TIN = counterpartyModel.TIN;
    }
    /// <summary>
    /// Обработка ИНН компании/банка из контрагента
    /// </summary>
    public virtual void ProcessCompanyTIN(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var message = CheckTIN(counterpartyModel.TIN, true);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($" >  ProcessPersonTIN > Message: {message}");
      }
      else if (counterparty.TIN != counterpartyModel.TIN)
        counterparty.TIN = counterpartyModel.TIN;
    }
    /// <summary>
    /// Обработка ИНН компании/банка из контрагента
    /// </summary>
    public virtual void ProcessCompanyTRRC(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      var company = Companies.As(counterparty);
      
      var message = CheckTRRC(counterpartyModel.TRRC);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($" >  ProcessCompanyTRRC > Message: {message}");
      }
      else if (company.TRRC != counterpartyModel.TRRC)
        company.TRRC = counterpartyModel.TRRC;
    }
    /// <summary>
    /// Обработать головную организацию контрагента
    /// </summary>
    public virtual void ProcessHeadCompany(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      ICompany company = Companies.As(counterparty);
      
      ICompany headCompany = GetHeadCompany(counterpartyModel, syncResult, isDebug);
      if (company == null && counterpartyModel.HeadCompany != null)
      {
        var message = sline.IntegrationCore.Resources.Counterparty_HeadCompanyNotFoundFormat(counterpartyModel.HeadCompany?.Id, counterpartyModel.HeadCompany?.ExternalId,
                                                                                             counterpartyModel.Entity?.Id, counterpartyModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessHeadCompany > Message: {message}");
      }
      if (company.HeadCompany != headCompany)
        company.HeadCompany = headCompany;
    }
    /// <summary>
    /// Получение головную организацию контрагента
    /// </summary>
    public virtual ICompany GetHeadCompany(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      ICompany company = null;
      
      var findModel = counterpartyModel.HeadCompany;
      if (findModel == null)
        return company;
      if (findModel.Id != null && findModel.Id.HasValue)
        company = Companies.GetAll(d => Equals(d.Id, findModel.Id.Value)).SingleOrDefault();
      if (company == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        company = Companies.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return company;
    }
    /// <summary>
    /// Обработка БИК
    /// </summary>
    public virtual void ProcessBIC(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, IBank bank, bool isDebug)
    {
      var message = CheckBIC(counterpartyModel.BIC);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessBic > Message: {message}");
      }
      else if (bank.BIC != counterpartyModel.BIC)
        bank.BIC = counterpartyModel.BIC;
    }
    /// <summary>
    /// Обработка SWIFT
    /// </summary>
    public virtual void ProcessSWIFT(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, IBank bank, bool isDebug)
    {
      var message = CheckSWIFT(counterpartyModel.SWIFT);
      if (!string.IsNullOrEmpty(message))
      {
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessBic > Message: {message}");
      }
      else if (bank.SWIFT != counterpartyModel.SWIFT)
        bank.SWIFT = counterpartyModel.SWIFT;
    }
    /// <summary>
    /// Обработать ответственного сотрудника за контрагента
    /// </summary>
    public virtual void ProcessResponsible(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      IEmployee responsible = GetResponsible(counterpartyModel, syncResult, isDebug);
      if (responsible == null && counterpartyModel.Responsible != null)
      {
        var message = sline.IntegrationCore.Resources.Counterparty_ResponsibleNotFoundFormat(counterpartyModel.Responsible?.Id, counterpartyModel.Responsible?.ExternalId,
                                                                                             counterpartyModel.Entity?.Id, counterpartyModel.Entity?.ExternalId);
        syncResult.Result = Constants.Module.SyncResult.Warning;
        AddMessage(syncResult, Constants.Module.MessageType.Info, message);
        if (isDebug)
          Logger.WithLogger("IntegrationCore").Debug($"ProcessResponsible > Message: {message}");
      }
      if (counterparty.Responsible != responsible)
        counterparty.Responsible = responsible;
    }
    /// <summary>
    /// Получение ответственного сотрудника для контрагента
    /// </summary>
    public virtual IEmployee GetResponsible(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, bool isDebug)
    {
      IEmployee responsible = null;
      
      var findModel = counterpartyModel.Responsible;
      if (findModel == null)
        return responsible;
      if (findModel.Id != null && findModel.Id.HasValue)
        responsible = Employees.GetAll(d => Equals(d.Id, findModel.Id.Value)).SingleOrDefault();
      if (responsible == null && !string.IsNullOrWhiteSpace(findModel.ExternalId))
        responsible = Employees.GetAll(d => Equals(d.ExternalId, findModel.ExternalId)).SingleOrDefault();
      
      return responsible;
    }
    /// <summary>
    /// Обработка кастомных свойств
    /// </summary>
    public virtual void ProcessObjectExtension(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty, bool isDebug)
    {
      // для реализации в перекрытиях
    }
    /// <summary>
    /// Сохранить контрагента
    /// </summary>
    public virtual void SaveCounterparty(Structures.Module.ICounterpartyModel counterpartyModel, Structures.Module.ISyncResult syncResult, ICounterparty counterparty)
    {
      if (counterparty == null)
        return;

      if (counterparty.State.IsChanged)
      {
        try
        {
          counterparty.Save();
        }
        catch (Sungero.Domain.Shared.Validation.ValidationException vExc)
        {
          var validationErrors = string.Join("; ", vExc.ValidationMessages);
          var message = sline.IntegrationCore.Resources.Entity_ValidationErrorFormat(counterpartyModel.Entity?.Id, counterpartyModel.Entity?.ExternalId, validationErrors);
          Logger.WithLogger("IntegrationCore").Error($"CreateOrUpdateCounterparty > Message: {message}, StackTrace: {vExc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
        catch (Exception exc)
        {
          var message = sline.IntegrationCore.Resources.Entity_SaveErrorFormat(counterpartyModel.Entity?.Id, counterpartyModel.Entity?.ExternalId, exc.Message);
          Logger.WithLogger("IntegrationCore").Error($"CreateOrUpdateCounterparty > Message: {message}, StackTrace: {exc.StackTrace}");
          syncResult.Result = Constants.Module.SyncResult.Error;
          AddMessage(syncResult, Constants.Module.MessageType.Critical, message);
        }
      }
    }
    
    #endregion
    
    #region Служебное
    
    #region Результаты синхронизации
    
    /// <summary>
    /// Создание структуры с результатом синхронизации
    /// </summary>
    public virtual Structures.Module.ISyncResult CreateResult(sline.IntegrationCore.Structures.Module.IEntityModel model)
    {
      var syncResult = Structures.Module.SyncResult.Create();
      syncResult.Entity = model;
      syncResult.Result = Constants.Module.SyncResult.Success;
      syncResult.Messages = new List<Structures.Module.ISyncResultMessage>();
      return syncResult;
    }
    /// <summary>
    /// Создание структуры с результатом синхронизации
    /// </summary>
    public virtual Structures.Module.ISyncResult CreateResult(long id)
    {
      var syncResult = Structures.Module.SyncResult.Create();
      syncResult.Entity = Structures.Module.EntityModel.Create();
      syncResult.Entity.Id = id;
      syncResult.Result = Constants.Module.SyncResult.Success;
      syncResult.Messages = new List<Structures.Module.ISyncResultMessage>();
      return syncResult;
    }
    /// <summary>
    /// Добавление сообщения в массив сообщений структуры синхронизации
    /// </summary>
    public virtual void AddMessage(Structures.Module.ISyncResult syncResult, string type, string message)
    {
      var syncResultMessage = Structures.Module.SyncResultMessage.Create();
      syncResultMessage.Type = type;
      syncResultMessage.Message = message;
      syncResult.Messages.Add(syncResultMessage);
    }
    
    #endregion
    
    #region Вызов коробочных проверок реквизитов
    
    /// <summary>
    /// Проверка ИНН
    /// </summary>
    public virtual string CheckTIN(string TIN, bool isCompany)
    {
      return Sungero.Parties.PublicFunctions.Counterparty.CheckTin(TIN, isCompany);
    }
    /// <summary>
    /// Проверка КПП
    /// </summary>
    public virtual string CheckTRRC(string TRRC)
    {
      return Sungero.Parties.PublicFunctions.CompanyBase.CheckTRRC(TRRC);
    }
    /// <summary>
    /// Проверка БИК
    /// </summary>
    public virtual string CheckBIC(string BIC)
    {
      return Sungero.Parties.PublicFunctions.Bank.CheckBicLength(BIC);
    }
    /// <summary>
    /// Проверка SWIFT
    /// </summary>
    public virtual string CheckSWIFT(string SWIFT)
    {
      return Sungero.Parties.PublicFunctions.Bank.CheckSwift(SWIFT);
    }
    
    #endregion
    
    /// <summary>
    /// Обработка состояния сущности
    /// </summary>
    public virtual Enumeration ProcessEntityStatus(string extStatus)
    {
      var status = Sungero.CoreEntities.DatabookEntry.Status.Active;
      switch (extStatus)
      {
        case "Active":
          status = Sungero.CoreEntities.DatabookEntry.Status.Active;
          break;
        case "Closed":
          status = Sungero.CoreEntities.DatabookEntry.Status.Closed;
          break;
        default:
          break;
      }
      return status;
    }
    
    /// <summary>
    /// Создание "тяжелого" тела запроса
    /// </summary>
    public virtual ILargeBody CreateLargeBody(IBaseRequest request, string body)
    {
      var largeBody = LargeBodies.Create();
      largeBody.Body = body;
      largeBody.Request = request;
      largeBody.Save();
      
      return largeBody;
    }
    
    #region Альтернативные способы работы с данными для асинхронных обработчиков
    
    /// <summary>
    /// Записать в БД идентификаторы, занятые конкретным асинхронным обработчиком. Запись в базу идет построчно
    /// </summary>
    /// <param name="guid">GUID асинхронного обработчика</param>
    /// <param name="ids">Строка с идентификаторами (через точку с запятой)</param>
    [Public]
    public virtual void InsertAsyncIds(string guid, string ids)
    {
      foreach (var element in ids.Split(';'))
      {
        var sqlCommand = string.Format(Queries.Module.InsertAsyncIds, guid, element);
        Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(sqlCommand);
      }
    }
    /// <summary>
    /// Получить из БД идентификаторы, занятые конкретным асинхронным обработчиком
    /// </summary>
    /// <param name="guid">GUID асинхронного обработчика</param>
    /// <returns>Список идентификаторов</returns>
    [Public]
    public virtual List<string> GetAsyncIds(string guid)
    {
      var sqlCommand = string.Format(Queries.Module.SelectAsyncIds, guid);
      var result = new List<string>();
      
      using (var connection = SQL.GetCurrentConnection())
      {
        using (var command = connection.CreateCommand())
        {
          try
          {
            command.CommandText = sqlCommand;
            using (var reader = command.ExecuteReader())
            {
              while (reader.Read())
                result.Add(reader.GetString(0));
              
              return result;
            }
          }
          catch (Exception ex)
          {
            Logger.WithLogger("IntegrationCore").Error("Error while getting request ids", ex);
          }
        }
      }
      return result;
    }
    
    #endregion
    
    #endregion
    
    #region Настройки
    
    /// <summary>
    /// Получаем значение параметра "Входящие запросы"
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual bool GetIncomingRequestSetting()
    {
      var captureIncRequest = false;
      try
      {
        var value = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.IncomingRequestParamName);
        if (value != null)
          captureIncRequest = Convert.ToBoolean(value);
      }
      catch
      { }
      return captureIncRequest;
    }
    /// <summary>
    /// Получаем значение параметра "Количество итераций"
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual int GetIterationMaxCountSetting()
    {
      var iteration = 5;
      try
      {
        var value = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.IterationMaxCountParamName);
        if (value != null)
          iteration = Convert.ToInt32(value);
      }
      catch
      { }
      return iteration;
    }
    /// <summary>
    /// Получаем значение параметра "Размер пакета"
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual int GetAsyncBatchSetting()
    {
      var batch = 100;
      try
      {
        var value = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.AsyncBatchParamName);
        if (value != null)
          batch = Convert.ToInt32(value);
      }
      catch
      { }
      return batch;
    }
    /// <summary>
    /// Получаем значение параметра "Режим отладки"
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual bool GetDebugModeSetting()
    {
      var debugMode = false;
      try
      {
        var value = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.DebugModeParamName);
        if (value != null)
          debugMode = Convert.ToBoolean(value);
      }
      catch
      { }
      return debugMode;
    }
    /// <summary>
    /// Получаем значение параметра "Время жизни запроса"
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual int GetLifeTimeSetting()
    {
      var lifeTime = 14;
      try
      {
        var value = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.LifeTimeParamName);
        if (value != null)
          lifeTime = Convert.ToInt32(value);
      }
      catch
      { }
      return lifeTime;
    }
    
    #endregion
    
    #region Функции для клиентской части
    
    [Public, Remote]
    public IRole GetIntegrationRole()
    {
      return Roles.GetAll(x => x.Sid == Constants.Module.RoleGuid.IntegrationRole).FirstOrDefault();
    }
    
    #endregion
  }
}