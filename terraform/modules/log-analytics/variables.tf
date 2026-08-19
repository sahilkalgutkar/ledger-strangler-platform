variable "name" {
  description = "Log Analytics workspace name"
  type        = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "sku" {
  description = "Log Analytics pricing tier"
  type        = string
  default     = "PerGB2018"
}

variable "retention_in_days" {
  type    = number
  default = 30
}

variable "tags" {
  type    = map(string)
  default = {}
}
