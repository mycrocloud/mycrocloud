export default interface IVariable {
  id: number;
  name: string;
  valueType: string;
  stringValue?: string;
  isSecret: boolean;
  createdAt: string;
  updatedAt?: string;
}
