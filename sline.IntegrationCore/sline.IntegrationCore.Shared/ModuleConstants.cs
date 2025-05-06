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
      // параметр Количество итераций
      public const string IterationMaxCountParamName = "IntCore_IterationMaxCount";
      public const string IterationMaxCountParamValue = "5";
      
      // параметр Размер пакета
      public const string AsyncBatchParamName = "IntCore_BatchCount";
      public const string AsyncBatchParamValue = "100";
      
      // параметр Отладочный режим
      public const string DebugModeParamName = "IntCore_DebugMode";
      public const string DebugModeParamValue = "false";
      
      // параметр Фиксация входящих запросов
      public const string IncomingRequestParamName = "IntCore_LogIncomingRequest";
      public const string IncomingRequestParamValue = "false";
      
      // параметр Время жизни запросов
      public const string LifeTimeParamName = "IntCore_LifeTime";
      public const string LifeTimeParamValue = "14";
    }
    
    /// <summary>
    /// Результат синхронизации сущности
    /// </summary>     
    [Sungero.Core.Public]
    public static class SyncResult
    {
      public const string Success = "Success";
      public const string Warning = "Warning";
      public const string Error = "Error";
    }
    
    /// <summary>
    /// Тип сообщения
    /// </summary>
    [Sungero.Core.Public]
    public static class MessageType
    {
      public const string Critical = "Critical";
      public const string Info = "Info";
    }
    
    /// <summary>
    /// Тип контрагента
    /// </summary>
    [Sungero.Core.Public]
    public static class CounterpartyType
    {
      public const string Person = "Person";
      public const string Bank = "Bank";
      public const string Company = "Company";
    }
  }
}