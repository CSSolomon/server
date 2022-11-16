﻿using System.Data;
using System.Data.SqlClient;
using Bit.Core.Entities;
using Bit.Core.Models.Data.Organizations;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Dapper;

namespace Bit.Infrastructure.Dapper.Repositories;

public class OrganizationRepository : Repository<Organization, Guid>, IOrganizationRepository
{
    public OrganizationRepository(GlobalSettings globalSettings)
        : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
    { }

    public OrganizationRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
    { }

    public async Task<Organization> GetByIdentifierAsync(string identifier)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<Organization>(
                "[dbo].[Organization_ReadByIdentifier]",
                new { Identifier = identifier },
                commandType: CommandType.StoredProcedure);

            return results.SingleOrDefault();
        }
    }

    public async Task<ICollection<Organization>> GetManyByEnabledAsync()
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<Organization>(
                "[dbo].[Organization_ReadByEnabled]",
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }

    public async Task<ICollection<Organization>> GetManyByUserIdAsync(Guid userId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<Organization>(
                "[dbo].[Organization_ReadByUserId]",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }

    public async Task<ICollection<Organization>> SearchAsync(string name, string userEmail, bool? paid,
        int skip, int take)
    {
        using (var connection = new SqlConnection(ReadOnlyConnectionString))
        {
            var results = await connection.QueryAsync<Organization>(
                "[dbo].[Organization_Search]",
                new { Name = name, UserEmail = userEmail, Paid = paid, Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            return results.ToList();
        }
    }

    public async Task UpdateStorageAsync(Guid id)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.ExecuteAsync(
                "[dbo].[Organization_UpdateStorage]",
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180);
        }
    }

    public async Task<ICollection<OrganizationAbility>> GetManyAbilitiesAsync()
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<OrganizationAbility>(
                "[dbo].[Organization_ReadAbilities]",
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }

    public async Task<Organization> GetByLicenseKeyAsync(string licenseKey)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var result = await connection.QueryAsync<Organization>(
                "[dbo].[Organization_ReadByLicenseKey]",
                new { LicenseKey = licenseKey },
                commandType: CommandType.StoredProcedure);

            return result.SingleOrDefault();
        }
    }
    
    public async Task<SelfHostedOrganizationDetails> GetSelfHostedOrganizationDetailsById(Guid id)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var result = await connection.QueryMultipleAsync(
                "[dbo].[Organization_ReadSelfHostedDetailsById]",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            var selfHostOrganization = result.Read<SelfHostedOrganizationDetails>().FirstOrDefault();
            if (selfHostOrganization == null)
            {
                return null;
            }
            
            var organizationUsers = result.Read<OrganizationUser>();
            var collectionCount = result.Read<int>();
            var groupCount = result.Read<int>();
            var policies = result.Read<Policy>();
            var ssoConfig = result.Read<SsoConfig>();
            var scimConnections = result.Read<OrganizationConnection>();

            selfHostOrganization.OrganizationUsers = organizationUsers;
            selfHostOrganization.CollectionCount = collectionCount.FirstOrDefault();
            selfHostOrganization.GroupCount = groupCount.FirstOrDefault();
            selfHostOrganization.Policies = policies;
            selfHostOrganization.SsoConfig = ssoConfig.FirstOrDefault();
            selfHostOrganization.ScimConnections = scimConnections;

            return selfHostOrganization;
        }
    }
}
