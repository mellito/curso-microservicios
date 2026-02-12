#!/bin/bash

RESOURCE_GROUP_NAME="rg-microservicios"
LOCATION="eastus2"
SB_NAMESPACE_NAME="sb-age-test"
CONTAINER_APPS_ENV_NAME="env-microservicios"

# Create a resource group
az group create --name $RESOURCE_GROUP_NAME --location $LOCATION

# Create a Service Bus namespace and services for messaging
az servicebus namespace create --resource-group $RESOURCE_GROUP_NAME --name $SB_NAMESPACE_NAME --location $LOCATION --sku Standard

az servicebus queue create --resource-group $RESOURCE_GROUP_NAME --namespace-name $SB_NAMESPACE_NAME --name "pickage"

az servicebus topic create --resource-group $RESOURCE_GROUP_NAME --namespace-name $SB_NAMESPACE_NAME --name "adultstopic"
az servicebus topic subscription create --resource-group $RESOURCE_GROUP_NAME --namespace-name $SB_NAMESPACE_NAME --topic-name "adultstopic" --name S1

az servicebus topic create --resource-group $RESOURCE_GROUP_NAME --namespace-name $SB_NAMESPACE_NAME --name "childrentopic"
az servicebus topic subscription create --resource-group $RESOURCE_GROUP_NAME --namespace-name $SB_NAMESPACE_NAME --topic-name "childrentopic" --name S1

az containerapp env create --name $CONTAINER_APPS_ENV_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --internal-only false
