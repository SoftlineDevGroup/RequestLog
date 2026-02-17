using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using sline.IntegrationCore.Structures.Module;

namespace sline.IntegrationCore.Isolated.IsoArea
{
  public partial class IsolatedFunctions
  {
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="syncResult">Структура результата синхронизации</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(ISyncResult syncResult)
    {
      return Serialize(syncResult);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="jobTitleModel">Структура должности</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IJobTitleModel jobTitleModel)
    {
      return Serialize(jobTitleModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="personModel">Структура персоны</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IPersonModel personModel)
    {
      return Serialize(personModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="departmentModel">Структура подразделения</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IDepartmentModel departmentModel)
    {
      return Serialize(departmentModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="businessUnitModel">Структура НОР</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IBusinessUnitModel businessUnitModel)
    {
      return Serialize(businessUnitModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="employeeModel">Структура сотрудника</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IEmployeeModel employeeModel)
    {
      return Serialize(employeeModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="counterpartyModel">Структура контрагента</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(ICounterpartyModel counterpartyModel)
    {
      return Serialize(counterpartyModel);
    }
    
    /// <summary>
    /// Преобразование структуры в JSON строку
    /// </summary>
    /// <param name="answer">Структура ответа внешних систем</param>
    /// <returns>JSON строка</returns>
    [Public]
    public virtual string SerializeStructure(IAnswersFromOtherSystems answer)
    {
      return Serialize(answer);
    }
    
    /// <summary>
    /// Сериализация объекта структуры
    /// </summary>
    /// <param name="structure">Структура</param>
    /// <returns>JSON строка</returns>
    public virtual string Serialize(object structure)
    {
      return JsonConvert.SerializeObject(structure, Formatting.Indented);
    }
    
    
  }
}