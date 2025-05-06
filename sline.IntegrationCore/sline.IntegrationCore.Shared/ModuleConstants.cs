using System;
using Sungero.Core;

namespace sline.IntegrationCore.Constants
{
  public static class Module
  {

    /// <summary>
    /// Идентификаторы ролей
    /// </summary>
    [Sungero.Core.Public]
    public static class RoleGuid
    {
      /// <summary>
      /// Идентификатор роли Ответственные за интеграцию
      /// </summary>
      public static readonly Guid IntegrationRole = Guid.Parse("aecd8703-f782-47f9-a7b5-5fdebe1e9e0c");
    }
    
    /// <summary>
    /// Параметры модуля
    /// </summary>
    [Sungero.Core.Public]
    public static class IntegrationParams
    {
      /// <summary>
      /// Параметр "Количество итераций отправки"
      /// </summary>
      public const string IterationMaxCountParamName = "IntCore_IterationMaxCount";
      /// <summary>
      /// Значение параметра "Количество итераций отправки"
      /// </summary>
      public const string IterationMaxCountParamValue = "5";
      
      /// <summary>
      /// Параметр "Размер пакета"
      /// </summary>
      public const string AsyncBatchParamName = "IntCore_BatchCount";
      /// <summary>
      /// Значение параметра "Размер пакета"
      /// </summary>
      public const string AsyncBatchParamValue = "100";
      
      /// <summary>
      /// Параметр "Режим отладки"
      /// </summary>
      public const string DebugModeParamName = "IntCore_DebugMode";
      /// <summary>
      /// Значение параметра "Режим отладки"
      /// </summary>
      public const string DebugModeParamValue = "false";
      
      /// <summary>
      /// Параметр "Фиксировать входящие запросы"
      /// </summary>
      public const string IncomingRequestParamName = "IntCore_LogIncomingRequest";
      /// <summary>
      /// Значение параметра "Фиксировать входящие запросы"
      /// </summary>
      public const string IncomingRequestParamValue = "false";
      
      /// <summary>
      /// Параметр "Хранить запросы (в днях)"
      /// </summary>
      public const string LifeTimeParamName = "IntCore_LifeTime";
      /// <summary>
      /// Значение параметра "Хранить запросы (в днях)"
      /// </summary>
      public const string LifeTimeParamValue = "14";
    }
    
    /// <summary>
    /// Результат синхронизации сущности
    /// </summary>
    [Sungero.Core.Public]
    public static class SyncResult
    {
      /// <summary>
      /// Успешно
      /// </summary>
      public const string Success = "Success";
      /// <summary>
      /// Есть замечания
      /// </summary>
      public const string Warning = "Warning";
      /// <summary>
      /// Ошибка
      /// </summary>
      public const string Error = "Error";
    }
    
    /// <summary>
    /// Тип сообщения
    /// </summary>
    [Sungero.Core.Public]
    public static class MessageType
    {
      /// <summary>
      /// Критичное
      /// </summary>
      public const string Critical = "Critical";
      /// <summary>
      /// Информационное
      /// </summary>
      public const string Info = "Info";
    }
    
    /// <summary>
    /// Тип контрагента
    /// </summary>
    [Sungero.Core.Public]
    public static class CounterpartyType
    {
      /// <summary>
      /// Персона
      /// </summary>
      public const string Person = "Person";
      /// <summary>
      /// Банк
      /// </summary>
      public const string Bank = "Bank";
      /// <summary>
      /// Организация
      /// </summary>
      public const string Company = "Company";
    }
  }
}