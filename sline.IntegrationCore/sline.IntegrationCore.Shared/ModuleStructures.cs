using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace sline.IntegrationCore.Structures.Module
{
  
  #region Исходящие запросы
  
  /// <summary>
  /// Сокращенная структура наполнения данными справочника OutRequest
  /// </summary>
  [Public]
  partial class ShortData
  {
    /// <summary>
    /// Тип сущности
    /// </summary>
    public string EntityType { get; set; }
    /// <summary>
    /// ИД сущности
    /// </summary>
    public long EntityId { get; set; }
    /// <summary>
    /// Имя записи
    /// </summary>
    public string Name { get; set; }
  }

  /// <summary>
  /// Основная структура наполнения данными справочника OutRequest
  /// </summary>
  [Public]
  partial class Data
  {
    /// <summary>
    /// ИД
    /// </summary>
    public long? Id { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Код подключения внешней системы
    /// </summary>
    public string SystemCode { get; set; }
    /// <summary>
    /// Код метода внешней системы
    /// </summary>
    public string MethodCode { get; set; }
    /// <summary>
    /// Тело запроса
    /// </summary>
    public string Body { get; set; }
    /// <summary>
    /// Тип метода
    /// </summary>
    public string MethodType { get; set; }
    /// <summary>
    /// Аутентификационная информация
    /// </summary>
    public string Auth { get; set; }
    /// <summary>
    /// Заголовки
    /// </summary>
    public List<sline.IntegrationCore.Structures.Module.IHeaders> Headers { get; set; }
    /// <summary>
    /// Признак немедленной отправки
    /// </summary>
    public bool? InstantResponse { get; set; }
    /// <summary>
    /// Guid типа сущности
    /// </summary>
    public string EntityType { get; set; }
    /// <summary>
    /// ИД типа сущности
    /// </summary>
    public long? EntityId { get; set; }
    /// <summary>
    /// Наименование записи
    /// </summary>
    public string Name { get; set; }
  }
  /// <summary>
  /// Заголовки запроса
  /// </summary>
  [Public]
  partial class Headers
  {
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; set; }
    /// <summary>
    /// Значение
    /// </summary>
    public string Value { get; set; }
  }
  
  /// <summary>
  /// Ответы внешних систем
  /// </summary>
  [Public(Isolated=true)]
  partial class AnswersFromOtherSystems
  {
    /// <summary>
    /// ИД исходящего запроса
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Название внешней системы
    /// </summary>
    public string SystemName {get; set; }
    /// <summary>
    /// Статус
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Код ответа
    /// </summary>
    public string StatusCode { get; set; }
    /// <summary>
    /// Ответ
    /// </summary>
    public string Answer { get; set; }
    /// <summary>
    /// Дата ответа
    /// </summary>
    public DateTime? DateTime { get; set; }
  }
  
  #endregion

  #region Входящие запросы
  
  /// <summary>
  /// Входящий запрос
  /// </summary>
  [Public]
  partial class IncomingRequestDto
  {
    /// <summary>
    /// Наименование
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Guid типа сущности
    /// </summary>
    public string EntityType { get; set; }
    /// <summary>
    /// ИД типа сущности
    /// </summary>
    public long? EntityId { get; set; }
    /// <summary>
    /// Внешний идентификатор
    /// </summary>
    public string ExternalId { get; set; }
    /// <summary>
    /// Идентификатор внешней системы
    /// </summary>
    public string ExtSystemId { get; set; }
    /// <summary>
    /// Тело запроса
    /// </summary>
    public string Body { get; set; }
    /// <summary>
    /// Ответ
    /// </summary>
    public string Answer { get; set; }
  }
  #endregion
  
  #region Синхронизация оргструктуры
  
  /// <summary>
  /// Поиск сущности
  /// </summary>
  [Public(Isolated=true)]
  partial class EntityModel
  {
    /// <summary>
    /// ИД Директум
    /// </summary>
    public long? Id { get; set; }
    /// <summary>
    /// Внешний ИД
    /// </summary>
    public string ExternalId { get; set; }
    /// <summary>
    /// ИД внешней системы
    /// </summary>
    public string ExtSystemId { get; set; }
  }
  
  /// <summary>
  /// Модель НОР
  /// </summary>
  [Public(Isolated=true)]
  partial class BusinessUnitModel
  {
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Наименование
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Юридическое наименование
    /// </summary>
    public string LegalName { get; set; }
    /// <summary>
    /// ИНН
    /// </summary>
    public string TIN { get; set; }
    /// <summary>
    /// КПП
    /// </summary>
    public string TRRC { get; set; }
    /// <summary>
    /// ОГРН
    /// </summary>
    public string PSRN { get; set; }
    /// <summary>
    /// Идентификаторы головнаой НОР
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadCompany { get; set; }
    /// <summary>
    /// Идентификаторы руководителя НОР
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel CEO { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  /// <summary>
  /// Модель подразделения
  /// </summary>
  [Public(Isolated=true)]
  partial class DepartmentModel
  {
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Наименование
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Идентификаторы НОР
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel BusinessUnit { get; set; }
    /// <summary>
    /// Идентификаторы головного подразделения
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadOffice { get; set; }
    /// <summary>
    /// Идентификаторы руководителя подразделения
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Manager { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  /// <summary>
  /// Модель сотрудника
  /// </summary>
  [Public(Isolated=true)]
  partial class EmployeeModel
  {
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Идентификаторы персоны
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Person { get; set; }
    /// <summary>
    /// Идентификаторы должности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel JobTitle { get; set; }
    /// <summary>
    /// Идентификаторы подразделения
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Department { get; set; }
    /// <summary>
    /// Телефон
    /// </summary>
    public string Phone { get; set; }
    /// <summary>
    /// Эл. адрес
    /// </summary>
    public string Email { get; set; }
    /// <summary>
    /// Уведомление о просроченных заданиях
    /// </summary>
    public bool? NeedNotifyExpiredAssignments { get; set; }
    /// <summary>
    /// Уведомление о новых заданиях
    /// </summary>
    public bool? NeedNotifyNewAssignments { get; set; }
    /// <summary>
    /// Уведомление в виде сводки
    /// </summary>
    public bool? NeedNotifyAssignmentsSummary { get; set; }
    /// <summary>
    /// Табельный номер
    /// </summary>
    public string PersonnelNumber { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  /// <summary>
  /// Модель должности
  /// </summary>
  [Public(Isolated=true)]
  partial class JobTitleModel
  {
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Наименование
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Идентификаторы подразделения
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Department { get; set; }
    
    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  /// <summary>
  /// Модель персоны
  /// </summary>
  [Public(Isolated=true)]
  partial class PersonModel
  {
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Фамилия
    /// </summary>
    public string LastName { get; set; }
    /// <summary>
    /// Имя
    /// </summary>
    public string FirstName { get; set; }
    /// <summary>
    /// Отчество
    /// </summary>
    public string MiddleName { get; set; }
    /// <summary>
    /// Пол
    /// </summary>
    public string Sex { get; set; }
    /// <summary>
    /// Дата рождения
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    /// <summary>
    /// ИНН
    /// </summary>
    public string TIN { get; set; }
    /// <summary>
    /// СНИЛС
    /// </summary>
    public string INILA { get; set; }

    /// <summary>
    /// Объект для кастомных свойств сущности. Принимает на вход json строчку
    /// </summary>
    public string ObjectExtension { get; set; }
  }
  
  #endregion
  
  #region Контрагенты
  
  /// <summary>
  /// Модель контрагента
  /// </summary>
  [Public(Isolated=true)]
  partial class CounterpartyModel
  {
    /// <summary>
    /// Тип сущности
    /// </summary>
    public string EntityType { get; set; }
    
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Наименование
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Юридическое наименование
    /// </summary>
    public string LegalName { get; set; }
    /// <summary>
    /// Признак нерезидента
    /// </summary>
    public bool? Nonresident { get; set; }
    /// <summary>
    /// Состояние
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Код
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Город
    /// </summary>
    public string City { get; set; }
    /// <summary>
    /// Регион
    /// </summary>
    public string Region { get; set; }
    /// <summary>
    /// Юридический адрес
    /// </summary>
    public string LegalAddress { get; set; }
    /// <summary>
    /// Почтовый адрес
    /// </summary>
    public string PostalAddress { get; set; }
    /// <summary>
    /// Телефоны
    /// </summary>
    public string Phones { get; set; }
    /// <summary>
    /// Эл. почта
    /// </summary>
    public string Email { get; set; }
    /// <summary>
    /// Сайт
    /// </summary>
    public string Homepage { get; set; }
    /// <summary>
    /// Идентификаторы сотрудника, ответственного за контрагента
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Responsible { get; set; }
    /// <summary>
    /// Идентификаторы головного контрагента
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel HeadCompany { get; set; }
    
    /// <summary>
    /// ИНН
    /// </summary>
    public string TIN { get; set; }
    /// <summary>
    /// КПП
    /// </summary>
    public string TRRC { get; set; }
    /// <summary>
    /// ОГРН
    /// </summary>
    public string PSRN { get; set; }
    /// <summary>
    /// ОКПО
    /// </summary>
    public string NCEO { get; set; }
    /// <summary>
    /// ОКВЭД
    /// </summary>
    public string NCEA { get; set; }
    /// <summary>
    /// СНИЛС
    /// </summary>
    public string INILA { get; set; }
    /// <summary>
    /// БИК
    /// </summary>
    public string BIC { get; set; }
    /// <summary>
    /// SWIFT
    /// </summary>
    public string SWIFT { get; set; }
    /// <summary>
    /// Примечание
    /// </summary>
    public string Note { get; set; }
    
    /// <summary>
    /// Фамилия
    /// </summary>
    public string LastName { get; set; }
    /// <summary>
    /// Имя
    /// </summary>
    public string FirstName { get; set; }
    /// <summary>
    /// Отчество
    /// </summary>
    public string MiddleName { get; set; }
    /// <summary>
    /// Дата рождения
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    /// <summary>
    /// Пол
    /// </summary>
    public string Sex { get; set; }
    
    /// <summary>
    /// Банковский счет
    /// </summary>
    public string Account { get; set; }
    /// <summary>
    /// Корр. счет
    /// </summary>
    public string CorrespondentAccount { get; set; }
    /// <summary>
    /// Банк
    /// </summary>
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
    /// <summary>
    /// Идентификаторы сущности
    /// </summary>
    public sline.IntegrationCore.Structures.Module.IEntityModel Entity { get; set; }
    /// <summary>
    /// Результат синхронизации
    /// </summary>
    public string Result { get; set; }
    /// <summary>
    /// Гиперссылка
    /// </summary>
    public string Hyperlink { get; set; }
    /// <summary>
    /// Сообщения
    /// </summary>
    public List<sline.IntegrationCore.Structures.Module.ISyncResultMessage> Messages { get; set; }
  }
  
  /// <summary>
  /// Сообщения синхронизации
  /// </summary>
  [Public(Isolated=true)]
  partial class SyncResultMessage
  {
    /// <summary>
    /// Тип сообщения
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string Message { get; set; }
  }
  
  #endregion
  
}