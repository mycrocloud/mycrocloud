export default interface IApp {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt?: string;
  state: string;
  version: string;
  domain: string;
}



export interface IAppIntegration {
  type: string,
  org: string,
  repoId: number,
  repo: string
}