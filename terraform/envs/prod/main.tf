terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.116"
    }
  }

  # Local state for this portfolio project. In a real prod environment this
  # would point at an azurerm backend (a storage account + container) instead,
  # so state is shared and locked across whoever/whatever runs Terraform.
}

provider "azurerm" {
  features {}
}

locals {
  tags = {
    project     = "ledger-strangler-platform"
    environment = var.environment
    managed_by  = "terraform"
  }
}

module "resource_group" {
  source   = "../../modules/resource-group"
  name     = "rg-${var.environment}-ledger-strangler"
  location = var.location
  tags     = local.tags
}

module "log_analytics" {
  source              = "../../modules/log-analytics"
  name                = "log-${var.environment}-ledger-strangler"
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = local.tags
}

module "acr" {
  source              = "../../modules/acr"
  name                = "acr${var.environment}ledgerstrangler"
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = local.tags
}

module "aks" {
  source                     = "../../modules/aks"
  name                       = "aks-${var.environment}-ledger-strangler"
  resource_group_name        = module.resource_group.name
  location                   = var.location
  dns_prefix                 = "ledger-strangler-${var.environment}"
  node_count                 = var.aks_node_count
  vm_size                    = var.aks_vm_size
  log_analytics_workspace_id = module.log_analytics.id
  acr_id                     = module.acr.id
  tags                       = local.tags
}
