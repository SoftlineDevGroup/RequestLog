using System;
using Sungero.Core;

namespace sline.IntegrationCore.Constants
{
  public static class Module
  {

    [Sungero.Core.Public]
    public static class RoleGuid
    {
      public static readonly Guid IntegrationRole = Guid.Parse("aecd8703-f782-47f9-a7b5-5fdebe1e9e0c");
    }
    
    [Sungero.Core.Public]
    public static class IntegrationParams
    {
      public const string IterationMaxCountParamName = "IntCore_IterationMaxCount";
      public const string IterationMaxCountParamValue = "5";
      
      public const string AsyncBatchParamName = "IntCore_BatchCount";
      public const string AsyncBatchParamValue = "100";
      
      public const string DebugModeParamName = "IntCore_DebugMode";
      public const string DebugModeParamValue = "false";
      
      public const string IncomingRequestParamName = "IntCore_LogIncomingRequest";
      public const string IncomingRequestParamValue = "false";
      
      public const string LifeTimeParamName = "IntCore_LifeTime";
      public const string LifeTimeParamValue = "14";
    }
    
    // Результат синхронизации сущности
    [Sungero.Core.Public]
    public static class SyncResult
    {
      public const string Success = "Success";
      public const string Warning = "Warning";
      public const string Error = "Error";
    }
    
    // Тип сообщения
    [Sungero.Core.Public]
    public static class MessageType
    {
      public const string Critical = "Critical";
      public const string Info = "Info";
    }
    
    [Sungero.Core.Public]
    public static class CounterpartyType
    {
      public const string Person = "Person";
      public const string Bank = "Bank";
      public const string Company = "Company";
    }
  }
}