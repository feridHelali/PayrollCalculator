import { getRepository } from 'typeorm';
import { ActualMonthSalary } from '../entities/ActualMonthSalary';

export class ActualMonthSalaryRepository {
  private repository = getRepository(ActualMonthSalary);

  async findAll(): Promise<ActualMonthSalary[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<ActualMonthSalary | undefined> {
    return this.repository.findOne({where:{actualMonthSalaryId:id}}) as Promise<ActualMonthSalary | undefined>;
  }

  async create(actualMonthSalaryData: Partial<ActualMonthSalary>): Promise<ActualMonthSalary> {
    return this.repository.save(actualMonthSalaryData);
  }

  async update(id: number, conventionData: Partial<ActualMonthSalary>): Promise<ActualMonthSalary | undefined> {
    await this.repository.update(id, conventionData);
    return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}