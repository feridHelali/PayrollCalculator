import { ConventionCollectiveRepository } from '../db/repositories/ConventionCollectiveRepository';
import { ConventionCollective } from '../db/entities/ConventionCollective';

export class ConventionCollectioveService {
  private repository = new ConventionCollectiveRepository();

  async getAllConventions(): Promise<ConventionCollective[]> {
    return this.repository.findAll();
  }

  async getConventionById(id: number): Promise<ConventionCollective | undefined> {
    return this.repository.findById(id);
  }

  async createConvention(conventionData: Partial<ConventionCollective>): Promise<ConventionCollective> {
    return this.repository.create(conventionData);
  }

  async updateConvention(id: number, conventionData: Partial<ConventionCollective>): Promise<ConventionCollective | undefined> {
    return this.repository.update(id, conventionData);
  }

  async deleteConvention(id: number): Promise<void> {
    return this.repository.delete(id);
  }
}