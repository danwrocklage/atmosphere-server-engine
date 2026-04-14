using AGame.Core.Staff.Models;

namespace AGame.Core.Staff;

/// <summary>
/// Service for administrators and their role politics
/// </summary>
public interface IStaffService
{
    #region Staff

    /// <summary>
    /// Get staffs by filter
    /// </summary>
    Task<List<StaffShortItem>> GetStaffs(StaffFilter filter);
    
    /// <summary>
    /// Get staff short information. Return null if it's not found
    /// </summary>
    Task<StaffShortItem> GetStaffShort(Guid id);
    
    /// <summary>
    /// Get staff full information. Return null if it's not found
    /// </summary>
    Task<StaffItem> GetStaff(Guid id);

    /// <summary>
    /// Create new staff user. Not activated
    /// </summary>
    Task<Guid?> Create(CreateStaff model);
    
    /// <summary>
    /// Edit staff user info
    /// </summary>
    Task Edit(Guid staffId, EditStaff model);
    
    /// <summary>
    /// Delete staff user
    /// </summary>
    Task Delete(Guid staffId, Guid staffInitiatorId);
    
    /// <summary>
    /// Approve new staff
    /// </summary>
    Task Activate(Guid staffId, Guid staffInitiatorId);

    /// <summary>
    /// Disable staff
    /// </summary>
    Task Deactivate(Guid staffId);

    /// <summary>
    /// Get role scopes by staff id
    /// </summary>
    Task<string[]> GetStaffRoleScopes(Guid staffId);
    
    /// <summary>
    /// Can staff be authenticated
    /// </summary>
    Task<bool?> CanBeAuthenticated(Guid staffId);

    #endregion

    #region Roles

    /// <summary>
    /// Get role scopes by role id
    /// </summary>
    Task<string[]> GetRoleScopes(Guid roleId);
    
    /// <summary>
    /// Get all roles by filter
    /// </summary>
    Task<List<StaffRoleItem>> GetRoles(StaffRoleFilter filter);
    
    /// <summary>
    /// Create new staff role
    /// </summary>
    /// <param name="name">New role name</param>
    /// <param name="scopes">New role accessed scopes</param>
    /// <param name="staffInitiatorId">Staff who create role</param>
    Task<Guid?> CreateRole(string name, string[] scopes, Guid staffInitiatorId);
    
    /// <summary>
    /// Edit already existed staff role
    /// </summary>
    /// <param name="roleId">Role id</param>
    /// <param name="name">New role name</param>
    /// <param name="scopes">New role accessed scopes</param>
    /// <param name="staffInitiatorId">Staff who create role</param>
    Task EditRole(Guid roleId, string name, string[] scopes, Guid staffInitiatorId);

    #endregion
}