import { CategoryRepository } from '../db/repositories/CategoryRepository';
import { Category } from '../db/entities/Category';

export class CategoryService {
  private repository = new CategoryRepository();

  async getAllCategories(): Promise<Category[]|undefined> {
    return this.repository.findAll();
  }

  async getCategoryById(id: number): Promise<Category | undefined> {
    return this.repository.findById(id);
  }

  async createCategory(categoryData: Partial<Category>): Promise<Category> {
    return this.repository.create(categoryData);
  }

  async updateCategory(id: number, categoryData: Partial<Category>): Promise<Category | undefined> {
    return this.repository.update(id, categoryData);
  }

  async deleteCategory(id: number): Promise<void> {
    return this.repository.delete(id);
  }
}