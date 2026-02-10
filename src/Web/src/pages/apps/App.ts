export interface IActiveDeployment {
  id: string;
  name?: string;
  status: string;
  createdAt: string;
}

export default interface IApp {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt?: string;
  state: string;
  version: string;
  domain: string;
  activeSpaDeployment?: IActiveDeployment;
  activeApiDeployment?: IActiveDeployment;
}



export interface IAppIntegration {
  type: string,
  org: string,
  repoId: number,
  repo: string
}