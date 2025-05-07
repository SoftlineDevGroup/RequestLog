using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.ExternalMethod;

namespace sline.IntegrationCore.Server
{
  partial class ExternalMethodFunctions
  {
    /// <summary>
    /// Создать новый метод внешней системы
    /// </summary>
    /// <returns>Созданный Метод внешней системы</returns>
    [Public, Remote]
    public static sline.IntegrationCore.IExternalMethod CreateMethod()
    {
      return sline.IntegrationCore.ExternalMethods.Create();
    }
    
    /// <summary>
    /// Получить все методы внешней системы по подключению к внешней системе
    /// </summary>
    /// <param name="connection">Подключение к внешним системам</param>
    /// <returns>Список методов внешней системы</returns>
    [Public, Remote]
    public static IQueryable<sline.IntegrationCore.IExternalMethod> GetMethods(sline.IntegrationCore.IExternalConnection connection)
    {
      return sline.IntegrationCore.ExternalMethods.GetAll(x => Equals(x.ExternalConnection, connection));
    }
  }
}