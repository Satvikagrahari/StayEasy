import { Pipe, PipeTransform } from '@angular/core';

export function formatINR(amount: number): string {
  return '₹' + Number(amount || 0).toLocaleString('en-IN');
}

@Pipe({
  name: 'inr',
  standalone: true
})
export class InrPipe implements PipeTransform {
  transform(amount: number): string {
    return formatINR(amount);
  }
}
