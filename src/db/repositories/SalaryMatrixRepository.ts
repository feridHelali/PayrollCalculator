import { getRepository } from 'typeorm';
import { SalaryMatrix } from '../entities/SalaryMatrix';

export class SalryMatixRepository {
  private repository = getRepository(SalaryMatrix);

  async findAll(): Promise<SalaryMatrix[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<SalaryMatrix | undefined > {
    return this.repository.findOne({where:{id}}) as Promise<SalaryMatrix | undefined>;
  }

  async create(conventionData: Partial<SalaryMatrix>): Promise<SalaryMatrix> {
    return this.repository.save(conventionData);
  }

  async update(id: number, conventionData: Partial<SalaryMatrix>): Promise<SalaryMatrix | undefined > {
    await this.repository.update(id, conventionData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}


