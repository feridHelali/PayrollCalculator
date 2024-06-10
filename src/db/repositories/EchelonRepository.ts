import { getRepository } from 'typeorm';
import { Echelon } from '../entities/Echelon';

export class EchelonRepository {
  private repository = getRepository(Echelon);

  async findAll(): Promise<Echelon[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<Echelon | undefined > {
    return this.repository.findOne({where:{id}}) as Promise<Echelon | undefined>;
  }

  async create(conventionData: Partial<Echelon>): Promise<Echelon> {
    return this.repository.save(conventionData);
  }

  async update(id: number, conventionData: Partial<Echelon>): Promise<Echelon | undefined > {
    await this.repository.update(id, conventionData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}


