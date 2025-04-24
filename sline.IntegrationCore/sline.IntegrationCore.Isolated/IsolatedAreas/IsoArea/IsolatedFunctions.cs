using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using sline.IntegrationCore.Structures.Module;

namespace sline.IntegrationCore.Isolated.IsoArea
{
  public class IsolatedFunctions
  {
    [Public]
    public virtual string SerializeStructure(ISyncResult syncResult)
    {
      return Serialize(syncResult);
    }
    
    [Public]
    public virtual string SerializeStructure(IJobTitleModel jobTitleModel)
    {
      return Serialize(jobTitleModel);
    }
    
    [Public]
    public virtual string SerializeStructure(IPersonModel personModel)
    {
      return Serialize(personModel);
    }
    
    [Public]
    public virtual string SerializeStructure(IDepartmentModel departmentModel)
    {
      return Serialize(departmentModel);
    }
    
    [Public]
    public virtual string SerializeStructure(IBusinessUnitModel businessUnitModel)
    {
      return Serialize(businessUnitModel);
    }
    
    [Public]
    public virtual string SerializeStructure(IEmployeeModel employeeModel)
    {
      return Serialize(employeeModel);
    }
    
    [Public]
    public virtual string SerializeStructure(ICounterpartyModel counterpartyModel)
    {
      return Serialize(counterpartyModel);
    }
    
    [Public]
    public virtual string SerializeStructure(IAnswersFromOtherSystems answer)
    {
      return Serialize(answer);
    }
    
    public virtual string Serialize(object structure)
    {
      return JsonConvert.SerializeObject(structure, Formatting.Indented);
    }
    
    
  }
}