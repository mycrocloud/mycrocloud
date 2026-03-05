resource "conohavps_instance" "server" {
  instance_name_tag = "mycrocloud"
  flavor_id         = "4f32f7c2-b55b-4d1d-a0c7-320f0feaf8f4"
  block_device = [
    {
      uuid = "dd3855ee-9217-4a5a-bdfe-6ce1b2ec93e4"
    },
  ]
}