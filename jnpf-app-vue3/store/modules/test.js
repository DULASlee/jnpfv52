import {
	defineStore
} from 'pinia';

export const useTestStore = defineStore({
	id: ' test',
	state: () => ({
		count: 0,
	}),
	actions: {
		increment() {
			this.count++;
		},
		decrement() {
			this.count--;
		},
	},
});