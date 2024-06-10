import { getRepository } from 'typeorm';
import { ConventionCollective } from '../entities/ConventionCollective';

export class ConventionCollectiveRepository {
  private repository = getRepository(ConventionCollective);

  async findAll(): Promise<ConventionCollective[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<ConventionCollective | undefined > {
    return this.repository.findOne({where:{id}}) as Promise<ConventionCollective | undefined>;
  }

  async create(conventionData: Partial<ConventionCollective>): Promise<ConventionCollective> {
    return this.repository.save(conventionData);
  }

  async update(id: number, conventionData: Partial<ConventionCollective>): Promise<ConventionCollective | undefined > {
    await this.repository.update(id, conventionData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}


