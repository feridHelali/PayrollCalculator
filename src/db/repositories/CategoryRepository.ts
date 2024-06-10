import { getRepository } from 'typeorm';
import { Category } from '../entities/Category';

export class CategoryRepository {
  private repository = getRepository(Category);

  async findAll(): Promise<Category[]> {
    return this.repository.find();
  }

  async findById(id: number): Promise<Category | undefined > {
    return this.repository.findOne({where:{id}}) as Promise<Category | undefined>;
  }

  async create(categoryData: Partial<Category>): Promise<Category> {
    return this.repository.save(categoryData);
  }

  async update(id: number, categoryData: Partial<Category>): Promise<Category | undefined > {
    await this.repository.update(id, categoryData);
      return this.findById(id);
  }

  async delete(id: number): Promise<void> {
    await this.repository.delete(id);
  }
}
