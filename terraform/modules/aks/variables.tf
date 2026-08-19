variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "dns_prefix" {
  type = string
}

variable "node_count" {
  type    = number
  default = 2
}

variable "vm_size" {
  type    = string
  default = "Standard_B2s"
}

variable "log_analytics_workspace_id" {
  description = "Workspace the cluster ships Container Insights logs/metrics to"
  type        = string
}

variable "acr_id" {
  description = "Registry the cluster's kubelet identity is granted AcrPull on"
  type        = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
