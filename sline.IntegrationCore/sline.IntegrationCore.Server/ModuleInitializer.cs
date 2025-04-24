using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace sline.IntegrationCore.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateRoles();
      GrantRightsToIntegrationRole();
      CreateDocflowParams();
      CreateModuleTables();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public virtual void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      
      CreateRole(IntegrationCore.Resources.RoleNameIntegration, IntegrationCore.Resources.DescriptionIntegration, Constants.Module.RoleGuid.IntegrationRole);
    }
    
    public virtual IRole CreateRole(string roleName, string roleDescription, Guid roleGuid)
    {
      InitializationLogger.DebugFormat("Init: Create Role {0}", roleName);
      var role = Roles.GetAll(r => r.Sid == roleGuid).FirstOrDefault();
      
      if (role == null)
      {
        role = Roles.Create();
        role.Name = roleName;
        role.Description = roleDescription;
        role.Sid = roleGuid;
        //role.IsSystem = true;
        role.Save();
      }
      else
      {
        if (role.Name != roleName)
        {
          InitializationLogger.DebugFormat("Role '{0}'(Sid = {1}) renamed as '{2}'", role.Name, role.Sid, roleName);
          role.Name = roleName;
          role.Save();
        }
        if (role.Description != roleDescription)
        {
          InitializationLogger.DebugFormat("Role '{0}'(Sid = {1}) update Description '{2}'", role.Name, role.Sid, roleDescription);
          role.Description = roleDescription;
          role.Save();
        }
      }
      return role;
    }
    
    /// <summary>
    /// Назначить права роли
    /// </summary>
    public virtual void GrantRightsToIntegrationRole()
    {
      InitializationLogger.Debug("Init: Grant rights on integrations operation to role");
      
      // выдача прав всем пользователям
      IntegrationCore.BaseRequests.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
      IntegrationCore.BaseRequests.AccessRights.Save();
      IntegrationCore.ExternalConnections.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      IntegrationCore.ExternalConnections.AccessRights.Save();
      IntegrationCore.ExternalMethods.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      IntegrationCore.ExternalMethods.AccessRights.Save();
      IntegrationCore.LargeBodies.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
      IntegrationCore.LargeBodies.AccessRights.Save();
      
      var role = Functions.Module.GetIntegrationRole();
      if (role == null)
        return;
      
      IntegrationCore.BaseRequests.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      IntegrationCore.BaseRequests.AccessRights.Save();
      IntegrationCore.ExternalConnections.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      IntegrationCore.ExternalConnections.AccessRights.Save();
      IntegrationCore.ExternalMethods.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      IntegrationCore.ExternalMethods.AccessRights.Save();
      IntegrationCore.LargeBodies.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      IntegrationCore.LargeBodies.AccessRights.Save();
    }
    
    public virtual void CreateDocflowParams()
    {
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.IterationMaxCountParamName) == null)
        Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.IterationMaxCountParamName,
                                                                          Constants.Module.IntegrationParams.IterationMaxCountParamValue);
      
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.AsyncBatchParamName) == null)
        Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.AsyncBatchParamName,
                                                                          Constants.Module.IntegrationParams.AsyncBatchParamValue);
      
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.DebugModeParamName) == null)
        Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.DebugModeParamName,
                                                                          Constants.Module.IntegrationParams.DebugModeParamValue);      
      
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.IncomingRequestParamName) == null)
        Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.IncomingRequestParamName,
                                                                          Constants.Module.IntegrationParams.IncomingRequestParamValue);
      
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.IntegrationParams.LifeTimeParamName) == null)
        Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.IntegrationParams.LifeTimeParamName,
                                                                          Constants.Module.IntegrationParams.LifeTimeParamValue);
      
    }
    
    public virtual void CreateModuleTables()
    {
      return;
      
      // для альтернативного варианта, необходимо создать в базе данных таблицу для хранения идентификаторов
      // Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.InsertAsyncIds);
    }
  }
}
