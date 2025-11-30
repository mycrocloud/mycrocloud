export default interface ITextStorage {
  id: number;
  name: string;
  description?: string;
  size: number;
  createdAt: string;
  updatedAt?: string;
}
