@description('The base name of resources')
param name string = resourceGroup().name

@description('The location of resources')
param location string = resourceGroup().location

@description('The principal object ID to assign permissions')
@secure()
param objectId string

resource vault 'Microsoft.KeyVault/vaults@2021-10-01' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: objectId
        permissions: {
          certificates: [
            'create'
            'get'
          ]
        }
      }
    ]
  }
}

output vaultUri string = vault.properties.vaultUri
