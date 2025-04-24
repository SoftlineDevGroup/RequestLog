using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace sline.IntegrationCore.Structures.Module
{
  
  #region Исходящие запросы
  
  [Public]
  partial class CreatedOutQuery
  {
    public long Id { get; set; }
    public string Status { get; set; }
  }
  
  /// <summary>
  /// Сокращенная структура наполнения данными справочника OutRequest
  /// </summary>
  [Public]
  partial class ShortData
  {
    public string EntityType { get; set; }
    public long EntityId { get; set; }
    public string Name { get; set; }
  }

  /// <summary>
  /// Основная структура наполнения данными справочника OutRequest
  /// </summary>
  [Public]
  partial class Data
  {
    public long? Id { get; set; }
    public string Status { get; set; }
    public string SystemCode { get; set; }
    public string MethodCode { get; set; }
    public string Body { get; set; }
    public string MethodType { get; set; }
    public string Auth { get; set; }
    public List<sline.IntegrationCore.Structures.Module.IHeaders> Headers { get; set; }
    public bool? InstantResponse { get; set; }
    public string EntityType { get; set; }
    public long? EntityId { get; set; }
    public string Name { get; set; }
  }
  [Public]
  partial class Headers
  {
    public string Key { get; set; }
    public string Value { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class AnswersFromOtherSystems
  {      
    public long Id { get; set; }
    public string SystemName {get; set; }
    public string Status { get; set; }
    public string StatusCode { get; set; }
    public string Answer { get; set; }
    public DateTime? DateTime { get; set; }
  }
  
  #endregion

  #region Входящие запросы
  
  [Public]
  partial class IncomingRequestDto
  {
    public string Name { get; set; }
    public string EntityType { get; set; }
    public long? EntityId { get; set; }
    public string ExternalId { get; set; }
    public string ExtSystemId { get; set; }
    public string Body { get; set; }
    public string Answer { get; set; }
  }
  #endregion
  
  #region Синхронизация оргструктуры
  
  [Public(Isolated=true)]
  partial class EntityModel
  {
    public long? Id { get; set; }
    public string ExternalId { get; set; }
    public string ExtSystemId { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class BusinessUnitModel
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Status { get; set; }
    public string Name { get; set; }
    public string LegalName { get; set; }
    public string TIN { get; set; }
    public string TRRC { get; set; }
    public string PSRN { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadCompany { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel CEO { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class DepartmentModel
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Status { get; set; }
    public string Name { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel BusinessUnit { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadOffice { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel Manager { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class EmployeeModel
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Status { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel Person { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel JobTitle { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel Department { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public bool? NeedNotifyExpiredAssignments { get; set; }
    public bool? NeedNotifyNewAssignments { get; set; }
    public bool? NeedNotifyAssignmentsSummary { get; set; }
    public string PersonnelNumber { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class JobTitleModel
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Status { get; set; }
    public string Name { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel Department { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class PersonModel
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Status { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string Sex { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string TIN { get; set; }
    public string INILA { get; set; }

    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  #endregion
  
  #region Контрагенты
  
  [Public(Isolated=true)]
  partial class CounterpartyModel
  {
    public string EntityType { get; set; }
    
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Name { get; set; }
    public string LegalName { get; set; }
    public bool? Nonresident { get; set; }
    public string Status { get; set; }
    public string Code { get; set; }
    public string City { get; set; }
    public string Region { get; set; }
    public string LegalAddress { get; set; }
    public string PostalAddress { get; set; }
    public string Phones { get; set; }
    public string Email { get; set; }
    public string Homepage { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel Responsible { get; set; }
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadCompany { get; set; }
    
    public string TIN { get; set; }
    public string TRRC { get; set; }
    public string PSRN { get; set; }
    public string NCEO { get; set; }
    public string NCEA { get; set; }
    public string INILA { get; set; }
    public string BIC { get; set; }
    public string SWIFT { get; set; }
    
    public string Note { get; set; }
    
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Sex { get; set; }
    
    public string Account { get; set; }
    public string CorrespondentAccount { get; set; }
    public string Bank { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  #endregion
  
  #region Ответные статусы сервиса интеграции
  
  /// <summary>
  /// Результат синхронизации сущности.
  /// </summary>
  [Public(Isolated=true)]
  partial class SyncResult
  {
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    public string Result { get; set; }
    public string Hyperlink { get; set; }
    public List<sline.IntegrationCore.Structures.Module.ISyncResultMessage> Messages { get; set; }
  }
  
  [Public(Isolated=true)]
  partial class SyncResultMessage
  {
    public string Type { get; set; }
    public string Message { get; set; }
  }
  
  #endregion
  
}