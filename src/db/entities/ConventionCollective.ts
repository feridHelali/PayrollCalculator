import { Entity, PrimaryGeneratedColumn, Column, OneToMany } from 'typeorm';
import { Category } from './Category';

@Entity()
export class ConventionCollective {
  @PrimaryGeneratedColumn()
  id!: number;

  @Column()
  name: string="";

  @Column()
  startDate: Date=new Date();

  @Column()
  endDate: Date=new Date();

  @OneToMany(() => Category, category => category.conventionCollective)
  categories: Category[]=[];
}